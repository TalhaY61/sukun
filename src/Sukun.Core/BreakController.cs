namespace Sukun.Core;

public enum Playback { Other, Playing, Paused }
public sealed record MediaState(string SessionId, string AppId, string Fingerprint, Playback Playback, bool CanPause, bool CanPlay, long Revision = 0);
public interface IMediaControl
{
    Task<IReadOnlyList<MediaState>> ReadAsync();
    Task<bool> PauseAsync(string sessionId);
    Task<bool> PlayAsync(string sessionId);
}
public enum BreakOutcome { Idle, Skipped, Paused, Resumed, Cancelled, Failed }
public sealed record AutomationDiagnostic(string Code, string Detail, string? SessionId = null, Playback? State = null);

// The host serializes calls. Wall time is supplied by its injectable TimeProvider.
public sealed class BreakController(IMediaControl media)
{
    private readonly Dictionary<string, DateTimeOffset> handled = [];
    private BreakWindow? active;
    private MediaState? owned;
    private DateTimeOffset? lastTick;
    public event Action<AutomationDiagnostic>? Diagnostic;
    private Func<string> message = () => L.Get("ControllerOff");
    public string Message => message();
    public BreakOutcome Outcome { get; private set; }
    public bool IsActive => active is not null;
    public DateTimeOffset? ActiveUntil => active?.End;

    public void Cancel(string reason) => Cancel(() => reason);
    public void Cancel(Func<string> reason)
    {
        active = null;
        owned = null;
        Set(BreakOutcome.Cancelled, "cancelled", reason);
    }

    // Explicit wall-clock changes start a new time traversal. Only future events are rearmed.
    // Already-running intervals are skipped, and an owned player is never started on a clock jump.
    public void RebaseClock(DateTimeOffset now, IReadOnlyList<BreakWindow> windows)
    {
        Cancel(() => L.Get("ClockRebased"));
        foreach (var key in handled.Where(p => p.Value > now).Select(p => p.Key).ToArray()) handled.Remove(key);
        foreach (var window in windows.Where(w => w.Start <= now)) handled[window.Key] = window.Start;
        lastTick = now;
        Diagnostic?.Invoke(new("clock-rebased", $"future-rearmed; now={now:O}"));
    }

    public async Task TickAsync(DateTimeOffset now, bool enabled, IReadOnlyList<BreakWindow> windows, string? preference)
    {
        try { await TickCoreAsync(now, enabled, windows, preference); }
        catch (Exception error)
        {
            active = null; owned = null;
            Set(BreakOutcome.Failed, "media-exception", () => L.Get("MediaSessionError"));
            Diagnostic?.Invoke(new("api-error", $"{error.GetType().Name}; hr=0x{error.HResult:X8}"));
        }
    }

    private async Task TickCoreAsync(DateTimeOffset now, bool enabled, IReadOnlyList<BreakWindow> windows, string? preference)
    {
        var previous = lastTick;
        lastTick = now;
        foreach (var key in handled.Where(p => p.Value < now.AddDays(-3)).Select(p => p.Key).ToArray()) handled.Remove(key);
        if (!enabled)
        {
            foreach (var window in windows.Where(w => w.Start <= now)) handled[window.Key] = window.Start;
            if (active is not null) Cancel(() => L.Get("DisabledNoResume"));
            return;
        }
        if (previous is null)
        {
            foreach (var window in windows.Where(w => w.Start <= now)) handled[window.Key] = window.Start;
            Set(BreakOutcome.Idle, "started", () => L.Get("ReadyPastSkipped"));
            return;
        }
        if (now < previous) { RebaseClock(now, windows); return; }
        if (active is not null && now - previous > TimeSpan.FromSeconds(20))
        {
            RebaseClock(now, windows);
            return;
        }
        if (active is not null)
        {
            foreach (var overlap in windows.Where(w => w.Start <= active.End && w.End > active.Start))
            {
                handled[overlap.Key] = overlap.Start;
                if (overlap.End > active.End) active = active with { End = overlap.End };
            }
            var current = (await media.ReadAsync()).SingleOrDefault(s => s.SessionId == owned?.SessionId);
            if (current is null || current.Fingerprint != owned?.Fingerprint || current.Playback != Playback.Paused || current.Revision != owned?.Revision)
            { Cancel(() => L.Get("PlayerChanged")); return; }
            if (now < active.End) return;
            var target = owned!;
            active = null; owned = null; // A failed request must never be retried as a later surprise.
            if (string.IsNullOrWhiteSpace(target.Fingerprint))
            { Set(BreakOutcome.Skipped, "resume-no-identity", () => L.Get("ResumeNoIdentity")); return; }
            var accepted = await media.PlayAsync(target.SessionId);
            var after = (await media.ReadAsync()).SingleOrDefault(s => s.SessionId == target.SessionId);
            var verified = accepted && after?.Playback == Playback.Playing && after.Fingerprint == target.Fingerprint;
            Set(verified ? BreakOutcome.Resumed : BreakOutcome.Failed, verified ? "resume-confirmed" : "resume-failed",
                () => verified ? L.Get("ResumeConfirmed") : L.Get("ResumeFailed"), target.SessionId, after?.Playback);
            return;
        }
        var due = windows.FirstOrDefault(w => !handled.ContainsKey(w.Key) && w.Start <= now && now < w.End);
        if (due is null) return;
        handled[due.Key] = due.Start;
        if (now - due.Start > TimeSpan.FromSeconds(15)) { Set(BreakOutcome.Skipped, "late", () => L.Get("LateBreakSkipped")); return; }
        var states = await media.ReadAsync();
        var candidates = states.Where(s => s.Playback == Playback.Playing &&
            (preference is null || s.AppId == preference || preference == "session:" + s.SessionId)).ToArray();
        if (candidates.Length != 1)
        {
            Set(BreakOutcome.Skipped, "selection-ambiguous", () => candidates.Length == 0
                ? L.Get("NoPlayingSource")
                : L.Get("AmbiguousSources"));
            return;
        }
        var selected = candidates[0];
        // CanPlay is normally false WHILE PLAYING (including Chrome). Capability flags are
        // advisory; explicit idempotent Pause/Play requests and their actual states are verified.
        Diagnostic?.Invoke(new("pause-request", $"pauseEnabled={selected.CanPause}; playEnabled={selected.CanPlay}", selected.SessionId, selected.Playback));
        var paused = await media.PauseAsync(selected.SessionId);
        var confirmation = (await media.ReadAsync()).SingleOrDefault(s => s.SessionId == selected.SessionId);
        if (!paused || confirmation?.Playback != Playback.Paused || confirmation.Fingerprint != selected.Fingerprint)
        {
            Set(BreakOutcome.Failed, "pause-failed", () => L.Get("PauseFailed"), selected.SessionId, confirmation?.Playback);
            return;
        }
        active = due;
        owned = confirmation;
        Set(BreakOutcome.Paused, "pause-confirmed", () => L.Get("PauseUntil", L.Prayer(due.Prayer), L.Time(due.End, true)), selected.SessionId, confirmation.Playback);
    }

    private void Set(BreakOutcome outcome, string code, Func<string> text, string? sessionId = null, Playback? state = null)
    {
        Outcome = outcome;
        message = text;
        Diagnostic?.Invoke(new(code, text(), sessionId, state));
    }
}
