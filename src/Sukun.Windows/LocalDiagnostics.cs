using System.IO;
using System.Text.Json;
using Sukun.Core;

namespace Sukun.Windows;

public sealed class LocalDiagnostics(string root, TimeProvider clock)
{
    private readonly object sync = new();
    public string DirectoryPath { get; } = Path.Combine(root, "logs");
    public void Write(string code, object? details = null)
    {
        try
        {
            lock (sync)
            {
                Directory.CreateDirectory(DirectoryPath);
                var path = Path.Combine(DirectoryPath, "diagnostics.log");
                if (File.Exists(path) && new FileInfo(path).Length > 256_000)
                    File.Move(path, Path.Combine(DirectoryPath, "diagnostics.previous.log"), true);
                File.AppendAllText(path, JsonSerializer.Serialize(new { utc = clock.GetUtcNow(), local = clock.GetLocalNow(), zone = TimeZoneInfo.Local.Id, code, details }) + Environment.NewLine);
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { /* Diagnostics must not stop automation. */ }
    }
    public void Error(string code, Exception error) => Write(code, new { type = error.GetType().Name, hr = $"0x{error.HResult:X8}" });
    public static string SafeApp(string id) => new[] { "spotify", "chrome", "msedge", "firefox", "vlc", "youtube" }
        .FirstOrDefault(name => id.Contains(name, StringComparison.OrdinalIgnoreCase)) ?? "other";
    public void Media(IReadOnlyList<MediaState> states) => Write("media-snapshot", states.Select(s => new
    { id = s.SessionId[..Math.Min(8, s.SessionId.Length)], app = SafeApp(s.AppId), state = s.Playback.ToString(), s.CanPause, s.CanPlay, s.Revision }));
}
