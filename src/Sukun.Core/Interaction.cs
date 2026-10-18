using System.Globalization;
using System.Text;

namespace Sukun.Core;

public static class SearchText
{
    public static string Normalize(string? value)
    {
        var result = new StringBuilder();
        foreach (var c in (value ?? "").Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                result.Append(c == 'ı' ? 'i' : char.ToLowerInvariant(c));
        return result.ToString().Trim();
    }
    public static bool Matches(string text, string query) => Normalize(text).Contains(Normalize(query), StringComparison.Ordinal);
}

public sealed class ClockMonitor(TimeProvider clock)
{
    private DateTimeOffset? previousWall;
    private long previousStamp;
    public bool Observe()
    {
        var wall = clock.GetUtcNow();
        var stamp = clock.GetTimestamp();
        var changed = previousWall.HasValue && Math.Abs(((wall - previousWall.Value) - clock.GetElapsedTime(previousStamp, stamp)).TotalSeconds) > 2;
        previousWall = wall;
        previousStamp = stamp;
        return changed;
    }
    public void Reset() { previousWall = clock.GetUtcNow(); previousStamp = clock.GetTimestamp(); }
}

public static class ScheduleWakeup
{
    public static TimeSpan Delay(DateTimeOffset now, IReadOnlyList<BreakWindow> windows, bool active, bool visible)
    {
        if (active) return TimeSpan.FromSeconds(1);
        var next = windows.FirstOrDefault(w => w.Start > now)?.Start;
        return TimeSpan.FromSeconds(Math.Clamp(next.HasValue ? (next.Value - now).TotalSeconds : 60, .1, visible ? 15 : 60));
    }
}

public static class PopupGeometry
{
    // WPF custom popup callbacks use device pixels; owner bounds use DIPs.
    public static (double X, double Y) Place(double pixelWidth, double pixelHeight, double pixelTargetHeight,
        double left, double top, double right, double bottom, double scaleX, double scaleY)
    {
        var width = pixelWidth / scaleX;
        var height = pixelHeight / scaleY;
        var targetHeight = pixelTargetHeight / scaleY;
        var x = Math.Clamp(0, left, Math.Max(left, right - width));
        var y = height <= bottom - targetHeight - 4 ? targetHeight + 3 : -height - 3;
        y = Math.Clamp(y, top + 3, Math.Max(top + 3, bottom - height - 3));
        return (x * scaleX, y * scaleY);
    }
}
