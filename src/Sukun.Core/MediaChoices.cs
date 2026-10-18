namespace Sukun.Core;

public sealed record MediaChoice(string Key, string? AppId, string? SessionId, string Label, string Detail = "")
{
    public string Tooltip => string.IsNullOrEmpty(Detail) ? Label : $"{Label}\n{Detail}";
    public override string ToString() => Label;
}

public static class MediaChoices
{
    public static string SelectedKey(Settings settings) => settings.PreferredSessionId is not null
        ? "session:" + settings.PreferredSessionId : settings.PreferredApp is not null ? "app:" + settings.PreferredApp : "auto";
    public static string? Target(Settings settings) => settings.PreferredSessionId is not null ? "session:" + settings.PreferredSessionId : settings.PreferredApp;

    public static IReadOnlyList<MediaChoice> Build(IReadOnlyList<MediaState> sessions, Settings settings, Func<string, string> appName)
    {
        var playing = sessions.Where(s => s.Playback == Playback.Playing).ToArray();
        var automaticLabel = playing.Length == 1 ? L.Get("AutomaticSource", appName(playing[0].AppId)) : L.Get("Automatic");
        var automaticDetail = playing.Length == 1 ? L.Get("AutoOnlyPlaying") : playing.Length == 0 ? L.Get("AutoNone") : L.Get("AutoMultiple") + ". " + L.Get("AutoSourceHint");
        var items = new List<MediaChoice> { new("auto", null, null, automaticLabel, automaticDetail) };
        var apps = sessions.Select(s => s.AppId).Distinct().OrderBy(appName).ThenBy(id => id, StringComparer.Ordinal).ToArray();
        foreach (var app in apps)
        {
            var live = sessions.Where(s => s.AppId == app).ToArray();
            var siblings = apps.Where(id => appName(id) == appName(app)).ToArray();
            var name = appName(app) + (siblings.Length > 1 ? $" ({Array.IndexOf(siblings, app) + 1})" : "");
            if (live.Length == 1)
            {
                // A single source needs one durable app choice, not an app + session duplicate.
                // Keep a still-live explicit pin intact until the user selects the durable row.
                var pinned = settings.PreferredSessionId == live[0].SessionId;
                items.Add(new(pinned ? "session:" + live[0].SessionId : "app:" + app, app,
                    pinned ? live[0].SessionId : null, name, Describe(live[0])));
            }
            else
            {
                // An existing app preference stays represented but ambiguity is explicit.
                if (settings.PreferredSessionId is null && settings.PreferredApp == app)
                    items.Add(new("app:" + app, app, null, name, L.Get("OpenSessions", live.Length)));
                for (var i = 0; i < live.Length; i++)
                    items.Add(new("session:" + live[i].SessionId, app, live[i].SessionId, $"{name} · {i + 1}", Describe(live[i])));
            }
        }
        // Unavailable saved preferences are shown as a placeholder by the host, never as a
        // selectable ghost row, and NEVER silently mapped to a new session or automatic mode.
        return items;
    }

    private static string Describe(MediaState session)
    {
        var title = session.Fingerprint.Split('\n')[0];
        var state = session.Playback == Playback.Playing ? L.Get("Playing") : session.Playback == Playback.Paused ? L.Get("Paused") : L.Get("NotReady");
        return title.Length == 0 ? state : $"{state} · {title}";
    }
}
