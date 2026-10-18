using System.IO;
using System.Text.Json;
using Sukun.Core;

namespace Sukun.Windows;

public sealed class LocalStore
{
    private readonly SemaphoreSlim writeGate = new(1, 1);
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    public string Root { get; }
    private string? warningKey;
    public string? Warning => warningKey is null ? null : L.Get(warningKey);
    public LocalStore(string? root = null)
    {
        Root = root ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sukun");
        Directory.CreateDirectory(Root);
        // Preserve preferences from the prototype after the product rename. No cloud transfer.
        if (root is null && !File.Exists(Path.Combine(Root, "settings.json")))
        {
            var legacy = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vakit" + "Molasi");
            if (Directory.Exists(legacy))
            {
                try
                {
                    // Commit settings last; a failed migration can be retried on next startup.
                    foreach (var file in Directory.EnumerateFiles(legacy, "*.json").OrderBy(p => Path.GetFileName(p) == "settings.json" ? 1 : 0))
                    {
                        var name = Path.GetFileName(file);
                        var destination = Path.Combine(Root, name);
                        if ((name == "settings.json" || System.Text.RegularExpressions.Regex.IsMatch(name, @"^times-\d+\.json$")) && !File.Exists(destination))
                            File.Copy(file, destination, false);
                    }
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                { warningKey = "MigrationWarning"; }
            }
        }
    }
    public Settings LoadSettings()
    {
        var path = Path.Combine(Root, "settings.json");
        if (!File.Exists(path)) return new Settings();
        try
        {
            var result = JsonSerializer.Deserialize<Settings>(File.ReadAllText(path)) ?? throw new InvalidDataException();
            result.Validate();
            return result;
        }
        catch (Exception e) when (e is IOException or InvalidDataException or JsonException or ArgumentException or UnauthorizedAccessException)
        {
            warningKey = "SettingsWarning";
            return new Settings();
        }
    }
    public Task SaveSettingsAsync(Settings settings)
    {
        settings.Validate();
        return WriteAsync("settings.json", settings);
    }
    public async Task<Timetable?> LoadTableAsync(string? id)
    {
        if (string.IsNullOrEmpty(id) || !id.All(char.IsAsciiDigit)) return null;
        var path = Path.Combine(Root, $"times-{id}.json");
        if (!File.Exists(path)) return null;
        try
        {
            var info = new FileInfo(path);
            if (info.Length > 2_000_000) throw new InvalidDataException();
            var table = JsonSerializer.Deserialize<Timetable>(await File.ReadAllTextAsync(path)) ?? throw new InvalidDataException();
            table.Validate();
            if (table.PlaceId != id) throw new InvalidDataException();
            return table;
        }
        catch (Exception e) when (e is IOException or InvalidDataException or JsonException or ArgumentException or UnauthorizedAccessException)
        { warningKey = "TimetableWarning"; return null; }
    }
    public Task SaveTableAsync(Timetable table) => WriteAsync($"times-{table.PlaceId}.json", table);
    private async Task WriteAsync<T>(string name, T value)
    {
        await writeGate.WaitAsync();
        try
        {
            var destination = Path.Combine(Root, name);
            var temporary = destination + ".tmp";
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(value, Json));
            File.Move(temporary, destination, true);
        }
        finally { writeGate.Release(); }
    }
}
