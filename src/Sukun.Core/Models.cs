using System.Globalization;

namespace Sukun.Core;

public enum Prayer { Fajr, Dhuhr, Asr, Maghrib, Isha }

public static class PrayerNames
{
    public static string Turkish(this Prayer prayer) => Enum.IsDefined(prayer)
        ? L.ForLanguage(prayer.ToString(), "tr") : throw new ArgumentOutOfRangeException(nameof(prayer));
}

public sealed record Place(string Id, string Province, string Name, bool IsCenter)
{
    public string DisplayName => IsCenter ? L.Get("CenterPlace", Name) : Name;
    public override string ToString() => DisplayName;
}

public sealed class Settings
{
    public string? PlaceId { get; set; }
    public bool Enabled { get; set; }
    public bool AutoRefresh { get; set; } = true;
    public string? PreferredApp { get; set; }
    public string? PreferredSessionId { get; set; }
    public string? Language { get; set; }
    public string Theme { get; set; } = "System";
    public bool AutoCheckSoftwareUpdates { get; set; }
    public DateTimeOffset? LastSoftwareCheckAttempt { get; set; }
    public string? LastNotifiedSoftwareVersion { get; set; }
    public int BeforeSeconds { get; set; } = 30;
    public int AfterMinutes { get; set; } = 7;
    public HashSet<Prayer> Prayers { get; set; } = [Prayer.Dhuhr, Prayer.Asr, Prayer.Maghrib, Prayer.Isha];
    public Dictionary<string, DateTimeOffset> LastFetchAttempts { get; set; } = [];

    public void Validate()
    {
        if (BeforeSeconds is < 0 or > 300 || AfterMinutes is < 1 or > 60 ||
            Prayers is null || Prayers.Any(p => !Enum.IsDefined(p)) ||
            LastFetchAttempts is null || Theme is not ("System" or "Light" or "Dark") ||
            (PlaceId is not null && (PlaceId.Length is < 1 or > 10 || !PlaceId.All(char.IsAsciiDigit))))
            throw L.Data("InvalidSettings");
    }
}

public sealed record DayTimes(DateOnly Date, string[] Clocks)
{
    public void Validate()
    {
        if (Clocks is null || Clocks.Length != 6) throw L.Data("SixTimesRequired");
        var previous = -1;
        foreach (var clock in Clocks)
        {
            if (!TimeOnly.TryParseExact(clock, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
                throw L.Data("InvalidTime");
            var minute = value.Hour * 60 + value.Minute;
            if (minute <= previous) throw L.Data("InvalidTimeOrder");
            previous = minute;
        }
    }

    public DateTimeOffset At(Prayer prayer)
    {
        var index = prayer switch { Prayer.Fajr => 0, Prayer.Dhuhr => 2, Prayer.Asr => 3, Prayer.Maghrib => 4, Prayer.Isha => 5, _ => -1 };
        var time = TimeOnly.ParseExact(Clocks[index], "HH:mm", CultureInfo.InvariantCulture);
        // Official Turkish civil clocks, independent of the PC's own timezone.
        return new DateTimeOffset(Date.ToDateTime(time, DateTimeKind.Unspecified), TimeSpan.FromHours(3));
    }
}

public sealed record Timetable(string PlaceId, DateTimeOffset FetchedAt, SortedDictionary<DateOnly, DayTimes> Days)
{
    public static DateOnly TurkeyDate(DateTimeOffset now) => DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(3)).DateTime);
    public int ContiguousDays(DateOnly start)
    {
        var count = 0;
        while (count < 1500 && Days.ContainsKey(start.AddDays(count))) count++;
        return count;
    }
    public void Validate()
    {
        if (string.IsNullOrEmpty(PlaceId) || !PlaceId.All(char.IsAsciiDigit) || Days is null || Days.Count is < 1 or > 1500)
            throw L.Data("InvalidTimetable");
        foreach (var (date, day) in Days)
        {
            if (day is null || date != day.Date) throw L.Data("TimetableDayMismatch");
            day.Validate();
        }
    }
}

public sealed record BreakWindow(string Key, Prayer Prayer, DateTimeOffset Start, DateTimeOffset End)
{
    public static IReadOnlyList<BreakWindow> Build(Timetable? table, Settings settings, DateTimeOffset now)
    {
        if (table is null || table.PlaceId != settings.PlaceId) return [];
        var today = Timetable.TurkeyDate(now);
        return table.Days.Values.Where(d => d.Date >= today.AddDays(-1) && d.Date <= today.AddDays(2))
            .SelectMany(day => settings.Prayers.Select(prayer => new BreakWindow(
                $"{table.PlaceId}/{day.Date:yyyy-MM-dd}/{prayer}", prayer,
                day.At(prayer).AddSeconds(-settings.BeforeSeconds), day.At(prayer).AddMinutes(settings.AfterMinutes))))
            .OrderBy(w => w.Start).ToList();
    }
}
