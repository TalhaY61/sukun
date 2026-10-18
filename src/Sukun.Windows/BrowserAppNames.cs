using System.IO;
using System.Runtime.InteropServices;

namespace Sukun.Windows;

public static class BrowserAppNames
{
    private static readonly Lazy<string[]> musicIds = new(ReadMusicIds);

    public static bool IsYouTubeMusic(string appId) =>
        appId.Contains("YouTubeMusic", StringComparison.OrdinalIgnoreCase) ||
        appId.Contains("YouTube Music", StringComparison.OrdinalIgnoreCase) ||
        musicIds.Value.Contains(appId, StringComparer.OrdinalIgnoreCase);

    private static string[] ReadMusicIds()
    {
        // Chrome can shorten its PWA ID in Windows. Read the EXACT registered AUMID,
        // not --app-id, a song title, or browser profile/history contents.
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        object? shell = null;
        try
        {
            var type = Type.GetTypeFromProgID("Shell.Application");
            if (type is null) return [];
            shell = Activator.CreateInstance(type);
            if (shell is null) return [];
            foreach (var location in new[] { Environment.SpecialFolder.StartMenu, Environment.SpecialFolder.CommonStartMenu })
            {
                var root = Environment.GetFolderPath(location);
                if (!Directory.Exists(root)) continue;
                var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint };
                foreach (var file in Directory.EnumerateFiles(root, "YouTube Music.lnk", options))
                {
                    object? folder = null;
                    object? item = null;
                    try
                    {
                        folder = ((dynamic)shell).NameSpace(Path.GetDirectoryName(file));
                        if (folder is null) continue;
                        item = ((dynamic)folder).ParseName(Path.GetFileName(file));
                        if (item is null) continue;
                        object? value = ((dynamic)item).ExtendedProperty("System.AppUserModel.ID");
                        if (value is string id && !string.IsNullOrWhiteSpace(id) && id.Length < 512) result.Add(id);
                    }
                    finally { Release(item); Release(folder); }
                }
            }
        }
        catch (Exception error) when (error is COMException or IOException or UnauthorizedAccessException or Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) { }
        finally { Release(shell); }
        return result.ToArray();
    }
    private static void Release(object? value) { if (value is not null && Marshal.IsComObject(value)) Marshal.FinalReleaseComObject(value); }
}
