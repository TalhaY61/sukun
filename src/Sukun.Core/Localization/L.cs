using System.Globalization;
using System.Resources;

namespace Sukun.Core;

/// <summary>Central UI translations; protocol keys and Turkey prayer clocks stay invariant.</summary>
public static class L
{
    private static readonly ResourceManager Resources = new("Sukun.Core.Localization.Strings", typeof(L).Assembly);
    public static string Language { get; private set; } = Normalize(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
    public static CultureInfo Culture => CultureInfo.GetCultureInfo(Language == "tr" ? "tr-TR" : "en-GB");
    public static string Normalize(string? language) => language?.StartsWith("tr", StringComparison.OrdinalIgnoreCase) == true ? "tr" : "en";
    public static void Apply(string? preference)
    {
        Language = Normalize(preference ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = Culture;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = Culture;
    }
    public static string Get(string key, params object?[] args)
    {
        var value = Resources.GetString(key, Culture) ?? key;
        return args.Length == 0 ? value : string.Format(Culture, value, args);
    }
    public static string ForLanguage(string key, string language) => Resources.GetString(key, CultureInfo.GetCultureInfo(language)) ?? key;
    public static string Prayer(Prayer prayer) => Get(prayer.ToString());
    public static string Time(DateTimeOffset time, bool seconds = false) => time.ToOffset(TimeSpan.FromHours(3)).ToString(seconds ? "HH:mm:ss" : "HH:mm", Culture);
    public static string Duration(TimeSpan time) => time.TotalHours >= 1
        ? Get("HoursMinutes", (int)time.TotalHours, time.Minutes)
        : Get("MinutesShort", Math.Max(1, (int)Math.Ceiling(time.TotalMinutes)));
    public static string Playback(Playback state) => Get(state == Sukun.Core.Playback.Playing ? "Playing" : state == Sukun.Core.Playback.Paused ? "Paused" : "NotReady");
    public static System.IO.InvalidDataException Data(string key)
    {
        var error = new System.IO.InvalidDataException(Get(key));
        error.Data["Sukun.LocalizationKey"] = key;
        return error;
    }
    public static string Error(Exception error) => error.Data["Sukun.LocalizationKey"] is string key ? Get(key) : Get("OperationFailed");
    public static IEnumerable<string> Keys => Resources.GetResourceSet(CultureInfo.InvariantCulture, true, true)!.Cast<System.Collections.DictionaryEntry>().Select(entry => (string)entry.Key);
}
