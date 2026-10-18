using Windows.Media.Control;
using Windows.ApplicationModel;
using Sukun.Core;

namespace Sukun.Windows;

public sealed class WindowsMedia : IMediaControl, IDisposable
{
    private GlobalSystemMediaTransportControlsSessionManager? manager;
    private readonly Dictionary<nint, Entry> entries = [];
    private readonly Dictionary<string, Entry> sessions = [];
    private string lastDiagnosticState = "";
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> displayNames = new(StringComparer.Ordinal);
    public LocalDiagnostics? Log { get; set; }
    public event Action? Changed;

    private sealed class Entry : IDisposable
    {
        public GlobalSystemMediaTransportControlsSession Session { get; }
        public string Id { get; } = Guid.NewGuid().ToString("N");
        private long revision;
        private GlobalSystemMediaTransportControlsSessionPlaybackStatus previousStatus;
        private TimeSpan position;
        public long Revision => Interlocked.Read(ref revision);
        public Entry(GlobalSystemMediaTransportControlsSession session)
        {
            Session = session;
            previousStatus = session.GetPlaybackInfo().PlaybackStatus;
            try { position = session.GetTimelineProperties().Position; }
            catch (Exception error) when (error is System.Runtime.InteropServices.COMException or InvalidOperationException) { position = TimeSpan.Zero; }
            session.PlaybackInfoChanged += PlaybackChanged;
            session.TimelinePropertiesChanged += TimelineChanged;
        }
        private void PlaybackChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            try
            {
                var status = sender.GetPlaybackInfo().PlaybackStatus;
                // Paused -> Playing is user/app intervention. Repeated flags/metadata updates are not.
                if (previousStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused &&
                    status != GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused) Interlocked.Increment(ref revision);
                previousStatus = status;
                position = sender.GetTimelineProperties().Position;
            }
            catch (Exception e) when (e is System.Runtime.InteropServices.COMException or InvalidOperationException) { Interlocked.Increment(ref revision); }
        }
        private void TimelineChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
        {
            try
            {
                var nowPosition = sender.GetTimelineProperties().Position;
                if (sender.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused &&
                    Math.Abs((nowPosition - position).TotalSeconds) > 1) Interlocked.Increment(ref revision);
                position = nowPosition;
            }
            catch (Exception e) when (e is System.Runtime.InteropServices.COMException or InvalidOperationException) { Interlocked.Increment(ref revision); }
        }
        public void Dispose()
        {
            Session.PlaybackInfoChanged -= PlaybackChanged;
            Session.TimelinePropertiesChanged -= TimelineChanged;
        }
    }

    public async Task InitializeAsync()
    {
        manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        manager.SessionsChanged += SessionsChanged;
        manager.CurrentSessionChanged += CurrentChanged;
    }
    private void SessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args) => Changed?.Invoke();
    private void CurrentChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args) => Changed?.Invoke();

    public async Task<IReadOnlyList<MediaState>> ReadAsync()
    {
        if (manager is null) return [];
        var live = manager.GetSessions().ToList();
        // Native COM identity survives managed projection wrappers returned by fresh enumerations.
        var identities = live.Select(s => ((WinRT.IWinRTObject)s).NativeObject.ThisPtr).ToHashSet();
        foreach (var old in entries.Keys.Where(key => !identities.Contains(key)).ToList())
        { var entry = entries[old]; sessions.Remove(entry.Id); entry.Dispose(); entries.Remove(old); }
        var result = new List<MediaState>();
        foreach (var session in live)
        {
            try
            {
                var key = ((WinRT.IWinRTObject)session).NativeObject.ThisPtr;
                if (!entries.TryGetValue(key, out var entry))
                { entry = new Entry(session); entries[key] = entry; sessions[entry.Id] = entry; }
                var info = session.GetPlaybackInfo();
                var fingerprint = "";
                try
                {
                    var properties = await session.TryGetMediaPropertiesAsync();
                    if (!string.IsNullOrWhiteSpace(properties.Title)) fingerprint = $"{properties.Title}\n{properties.Artist}\n{properties.AlbumTitle}";
                }
                catch (Exception error) when (error is System.Runtime.InteropServices.COMException or InvalidOperationException)
                { Log?.Error("media-metadata-unavailable", error); }
                var status = info.PlaybackStatus switch
                {
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => Playback.Playing,
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => Playback.Paused,
                    _ => Playback.Other
                };
                result.Add(new MediaState(entry.Id, session.SourceAppUserModelId, fingerprint, status,
                    info.Controls.IsPauseEnabled, info.Controls.IsPlayEnabled, entry.Revision));
            }
            catch (Exception error) when (error is System.Runtime.InteropServices.COMException or InvalidOperationException)
            { Log?.Error("media-read-failed", error); }
        }
        var diagnosticState = string.Join(';', result.Select(s => $"{s.SessionId}:{s.Playback}:{s.CanPause}:{s.CanPlay}:{s.Revision}"));
        if (diagnosticState != lastDiagnosticState) { lastDiagnosticState = diagnosticState; Log?.Media(result); }
        return result;
    }
    public Task<bool> PauseAsync(string id) => SendAsync(id, true);
    public Task<bool> PlayAsync(string id) => SendAsync(id, false);
    private async Task<bool> SendAsync(string id, bool pause)
    {
        if (!sessions.TryGetValue(id, out var entry)) return false;
        try
        {
            var accepted = pause ? await entry.Session.TryPauseAsync() : await entry.Session.TryPlayAsync();
            var expected = pause ? GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused : GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            var verified = false;
            // Bounded command acknowledgement; no polling while idle and no simulated key toggles.
            for (var i = 0; accepted && i < 20; i++)
            {
                if (entry.Session.GetPlaybackInfo().PlaybackStatus == expected) { verified = true; break; }
                await Task.Delay(100);
            }
            Log?.Write("media-command", new { session = id[..8], command = pause ? "pause" : "play", accepted, verified });
            return verified;
        }
        catch (Exception error) when (error is System.Runtime.InteropServices.COMException or InvalidOperationException)
        { Log?.Error("media-command-failed", error); return false; }
    }
    public void Dispose()
    {
        if (manager is not null) { manager.SessionsChanged -= SessionsChanged; manager.CurrentSessionChanged -= CurrentChanged; }
        foreach (var entry in entries.Values) entry.Dispose();
        entries.Clear(); sessions.Clear();
    }
    public static string DisplayName(string appId)
    {
        if (displayNames.TryGetValue(L.Language + ":" + appId, out var cached)) return cached;
        var name = ResolveDisplayName(appId);
        if (displayNames.Count < 128) displayNames.TryAdd(L.Language + ":" + appId, name);
        return name;
    }
    private static string ResolveDisplayName(string appId)
    {
        if (BrowserAppNames.IsYouTubeMusic(appId)) return "YT Music";
        if (appId.Contains("chrome", StringComparison.OrdinalIgnoreCase)) return "Chrome";
        if (appId.Contains("msedge", StringComparison.OrdinalIgnoreCase)) return "Edge";
        try
        {
            var name = AppInfo.GetFromAppUserModelId(appId)?.DisplayInfo.DisplayName;
            if (!string.IsNullOrWhiteSpace(name) && !name.Contains('!') && name.Length < 80) return name;
        }
        catch (Exception error) when (error is System.Runtime.InteropServices.COMException or ArgumentException) { }
        if (appId.Contains("YouTube Music", StringComparison.OrdinalIgnoreCase) || appId.Contains("YouTubeMusic", StringComparison.OrdinalIgnoreCase)) return "YouTube Music";
        if (appId.Contains("Spotify", StringComparison.OrdinalIgnoreCase)) return "Spotify";
        if (appId.Contains("chrome", StringComparison.OrdinalIgnoreCase)) return "Google Chrome";
        if (appId.Contains("msedge", StringComparison.OrdinalIgnoreCase)) return "Microsoft Edge";
        if (appId.Contains("firefox", StringComparison.OrdinalIgnoreCase)) return "Firefox";
        if (appId.Contains("vlc", StringComparison.OrdinalIgnoreCase)) return "VLC";
        return L.Get("GenericPlayer");
    }
}
