using System.Text.Json;
using System.Text.RegularExpressions;

namespace Sukun.Core;

public sealed record GitHubRepository(string Owner, string Name)
{
    public Uri Page => new($"https://github.com/{Owner}/{Name}");
    public Uri LatestReleaseApi => new($"https://api.github.com/repos/{Owner}/{Name}/releases/latest");

    public static GitHubRepository? Parse(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) || !uri.IsDefaultPort ||
            uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0) return null;
        var parts = uri.AbsolutePath.Trim('/').Split('/');
        if (parts.Length != 2) return null;
        var owner = parts[0];
        var name = parts[1].EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? parts[1][..^4] : parts[1];
        if (!Regex.IsMatch(owner, @"^[A-Za-z0-9][A-Za-z0-9-]{0,38}$") ||
            !Regex.IsMatch(name, @"^[A-Za-z0-9_.-]{1,100}$") || name is "." or "..") return null;
        return new(owner, name);
    }

    public bool IsReleasePage(Uri uri) => uri.Scheme == "https" && uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) &&
        uri.IsDefaultPort && uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0 &&
        uri.AbsolutePath.StartsWith($"/{Owner}/{Name}/releases/tag/", StringComparison.OrdinalIgnoreCase) &&
        uri.AbsolutePath.Length > $"/{Owner}/{Name}/releases/tag/".Length;
}

public sealed record SoftwareRelease(Version Version, string Tag, Uri Page)
{
    // Stable release channel. Previews, malformed versions and foreign URLs never become updates.
    public static SoftwareRelease Parse(string json, GitHubRepository repository)
    {
        if (json.Length > 1_000_000) throw L.Data("ReleaseResponseTooLarge");
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.GetProperty("draft").GetBoolean() || root.GetProperty("prerelease").GetBoolean())
            throw L.Data("UnstableRelease");
        var tag = root.GetProperty("tag_name").GetString() ?? "";
        if (!Regex.IsMatch(tag, @"^[vV]?\d+\.\d+\.\d+$") ||
            !Version.TryParse(tag.TrimStart('v', 'V'), out var version))
            throw L.Data("UnknownVersion");
        if (!Uri.TryCreate(root.GetProperty("html_url").GetString(), UriKind.Absolute, out var page) || !repository.IsReleasePage(page) ||
            !page.AbsolutePath.Equals($"/{repository.Owner}/{repository.Name}/releases/tag/{Uri.EscapeDataString(tag)}", StringComparison.OrdinalIgnoreCase))
            throw L.Data("UntrustedRelease");
        return new(version, tag, page);
    }

    public bool IsNewerThan(Version current, bool currentIsPrerelease = false)
    {
        var comparison = Normalize(Version).CompareTo(Normalize(current));
        return comparison > 0 || currentIsPrerelease && comparison == 0;
    }
    private static Version Normalize(Version version) => new(version.Major, version.Minor, Math.Max(0, version.Build));
}
