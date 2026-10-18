using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace Sukun.Core;

public static class DiyanetParser
{
    private static readonly string[] Months = ["Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"];
    private static MatchCollection Matches(string text, string pattern) => Regex.Matches(text, pattern,
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(2));
    private static string Text(string html) => Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]*>", " ",
        RegexOptions.None, TimeSpan.FromSeconds(1))), @"\s+", " ", RegexOptions.None, TimeSpan.FromSeconds(1)).Trim();

    public static Timetable Parse(string html, string placeId, DateTimeOffset now)
    {
        if (html.Length > 4_000_000) throw L.Data("ResponseTooLarge");
        var identities = Matches(html, @"\bvar\s+ilceId\s*=\s*(\d+)\s*;");
        if (identities.Count != 1 || identities[0].Groups[1].Value != placeId)
            throw L.Data("WrongDiyanetPlace");
        var days = new SortedDictionary<DateOnly, DayTimes>();
        foreach (Match table in Matches(html, @"<table\b[^>]*>([\s\S]*?)</table>"))
        foreach (Match row in Matches(table.Groups[1].Value, @"<tr\b[^>]*>([\s\S]*?)</tr>"))
        {
            var cells = Matches(row.Groups[1].Value, @"<td\b[^>]*>([\s\S]*?)</td>")
                .Select(m => Text(m.Groups[1].Value)).ToArray();
            if (cells.Length != 8) continue;
            var date = Regex.Match(cells[0], @"^(\d{1,2})\s+(\S+)\s+(\d{4})(?:\s|$)", RegexOptions.None, TimeSpan.FromSeconds(1));
            if (!date.Success) continue;
            var month = Array.IndexOf(Months, date.Groups[2].Value) + 1;
            if (month == 0) throw L.Data("UnknownMonth");
            var key = new DateOnly(int.Parse(date.Groups[3].Value, CultureInfo.InvariantCulture), month,
                int.Parse(date.Groups[1].Value, CultureInfo.InvariantCulture));
            var day = new DayTimes(key, cells[2..]);
            day.Validate();
            if (days.TryGetValue(key, out var existing) && !existing.Clocks.SequenceEqual(day.Clocks))
                throw L.Data("ConflictingTimes");
            days[key] = day;
        }
        var result = new Timetable(placeId, now, days);
        result.Validate();
        if (result.ContiguousDays(Timetable.TurkeyDate(now)) < 14)
            throw L.Data("IncompleteTimetable");
        return result;
    }

    public static Timetable Merge(Timetable? previous, Timetable incoming)
    {
        incoming.Validate();
        if (previous is null || previous.PlaceId != incoming.PlaceId) return incoming;
        var cutoff = TurkeyCutoff(incoming.FetchedAt);
        var days = new SortedDictionary<DateOnly, DayTimes>(previous.Days.Where(p => p.Key >= cutoff).ToDictionary());
        foreach (var pair in incoming.Days) days[pair.Key] = pair.Value;
        return incoming with { Days = days };
    }
    private static DateOnly TurkeyCutoff(DateTimeOffset now) => Timetable.TurkeyDate(now).AddDays(-7);
}
