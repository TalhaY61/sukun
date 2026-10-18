namespace Sukun.Core;

public sealed record PrayerOverview(Prayer? NextPrayer, DateTimeOffset? NextAt, BreakWindow? NextAutomaticBreak)
{
    public static PrayerOverview Build(Timetable? table, Settings settings, DateTimeOffset now)
    {
        if (table is null || table.PlaceId != settings.PlaceId) return new(null, null, null);
        var today = Timetable.TurkeyDate(now);
        var upcoming = table.Days.Values.Where(d => d.Date >= today && d.Date <= today.AddDays(2))
            .SelectMany(d => Enum.GetValues<Prayer>().Select(p => (Prayer: p, At: d.At(p))))
            .Where(p => p.At > now).OrderBy(p => p.At).ToArray();
        var next = upcoming.FirstOrDefault();
        return new(upcoming.Length > 0 ? next.Prayer : null, upcoming.Length > 0 ? next.At : null,
            settings.Enabled ? BreakWindow.Build(table, settings, now).FirstOrDefault(w => w.Start > now) : null);
    }
}
