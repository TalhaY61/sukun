using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Sukun.Core;

public sealed record GitHubRepository(string Owner, string Name)
{
    public Uri Page => new($"https://github.com/{Owner}/{Name}");
    public Uri LatestReleaseApi => new($"https://api.github.com/repos/{Owner}/{Name}/releases/latest");
    public Uri ReleasesApi => new($"https://api.github.com/repos/{Owner}/{Name}/releases?per_page=30");

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

// SemVer precedence: beta.10 > beta.9, rc > beta, stable > prerelease.
// Build metadata does not make an already installed version newer.
public sealed record UpdateVersion(Version Core, string? Prerelease) : IComparable<UpdateVersion>
{
    public static UpdateVersion? Parse(string? value)
    {
        if (value is null || value.Length > 100) return null;
        var match = Regex.Match(value, @"^[vV]?(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$");
        if (!match.Success || !Version.TryParse($"{match.Groups[1].Value}.{match.Groups[2].Value}.{match.Groups[3].Value}", out var core)) return null;
        var preview = match.Groups[4].Success ? match.Groups[4].Value : null;
        if (preview is not null && preview.Split('.').Any(p => Numeric(p) && p.Length > 1 && p[0] == '0')) return null;
        return new(core, preview);
    }

    public int CompareTo(UpdateVersion? other)
    {
        if (other is null) return 1;
        var core = Core.CompareTo(other.Core);
        if (core != 0) return core;
        if (Prerelease is null) return other.Prerelease is null ? 0 : 1;
        if (other.Prerelease is null) return -1;
        var left = Prerelease.Split('.'); var right = other.Prerelease.Split('.');
        for (var i = 0; i < Math.Min(left.Length, right.Length); i++)
        {
            var ln = Numeric(left[i]); var rn = Numeric(right[i]);
            var compare = ln && rn ? left[i].Length.CompareTo(right[i].Length) : ln != rn ? ln ? -1 : 1 : 0;
            if (compare == 0) compare = string.CompareOrdinal(left[i], right[i]);
            if (compare != 0) return compare;
        }
        return left.Length.CompareTo(right.Length);
    }

    private static bool Numeric(string value) => value.All(char.IsAsciiDigit);
}

public sealed record SoftwareRelease(Version Version, string Tag, Uri Page)
{
    public UpdateVersion ReleaseVersion => UpdateVersion.Parse(Tag) ?? new(Version, null);

    public static SoftwareRelease Parse(string json, GitHubRepository repository, bool includePrereleases = false)
    {
        if (json.Length > 1_000_000) throw L.Data("ReleaseResponseTooLarge");
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var prerelease = root.GetProperty("prerelease").GetBoolean();
        if (root.GetProperty("draft").GetBoolean() || prerelease && !includePrereleases) throw L.Data("UnstableRelease");
        var tag = root.GetProperty("tag_name").GetString() ?? "";
        var version = UpdateVersion.Parse(tag);
        if (version is null || prerelease != (version.Prerelease is not null)) throw L.Data("UnknownVersion");
        if (!Uri.TryCreate(root.GetProperty("html_url").GetString(), UriKind.Absolute, out var page) || !repository.IsReleasePage(page) ||
            !page.AbsolutePath.Equals($"/{repository.Owner}/{repository.Name}/releases/tag/{Uri.EscapeDataString(tag)}", StringComparison.OrdinalIgnoreCase))
            throw L.Data("UntrustedRelease");
        return new(version.Core, tag, page);
    }

    public static SoftwareRelease? FindUpdate(string json, GitHubRepository repository, string currentVersion)
    {
        if (json.Length > 1_000_000) throw L.Data("ReleaseResponseTooLarge");
        var current = UpdateVersion.Parse(currentVersion) ?? throw L.Data("UnknownVersion");
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array)) throw L.Data("InvalidUpdateResponse");
        var candidates = root.ValueKind == JsonValueKind.Array ? root.EnumerateArray().ToArray() : [root];
        SoftwareRelease? best = null;
        foreach (var item in candidates)
        {
            try
            {
                var release = Parse(item.GetRawText(), repository, current.Prerelease is not null);
                if (release.ReleaseVersion.CompareTo(current) <= 0 ||
                    !DateTimeOffset.TryParse(item.GetProperty("published_at").GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
                    !HasWindowsAsset(item, repository, release.Tag)) continue;
                if (best is null || release.ReleaseVersion.CompareTo(best.ReleaseVersion) > 0) best = release;
            }
            catch (Exception error) when (error is InvalidDataException or JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
            { /* One draft/malformed/untrusted entry must not hide other valid releases. */ }
        }
        return best;
    }

    private static bool HasWindowsAsset(JsonElement release, GitHubRepository repository, string tag)
    {
        if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array) return false;
        foreach (var asset in assets.EnumerateArray())
        {
            if (asset.ValueKind != JsonValueKind.Object ||
                !asset.TryGetProperty("name", out var name) || name.GetString() != "Sukun-win-x64.exe" ||
                !asset.TryGetProperty("state", out var state) || state.GetString() != "uploaded" ||
                !asset.TryGetProperty("size", out var size) || !size.TryGetInt64(out var bytes) || bytes <= 0 ||
                !asset.TryGetProperty("browser_download_url", out var download) || !Uri.TryCreate(download.GetString(), UriKind.Absolute, out var uri)) continue;
            if (uri.Scheme == "https" && uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) && uri.IsDefaultPort &&
                uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0 &&
                uri.AbsolutePath.Equals($"/{repository.Owner}/{repository.Name}/releases/download/{Uri.EscapeDataString(tag)}/Sukun-win-x64.exe", StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    public bool IsNewerThan(Version current, bool currentIsPrerelease = false)
    {
        var normalized = new Version(current.Major, current.Minor, Math.Max(0, current.Build));
        var comparison = Version.CompareTo(normalized);
        return comparison > 0 || currentIsPrerelease && comparison == 0 && ReleaseVersion.Prerelease is null;
    }
}

public static class SoftwareUpdateSchedule
{
    public static bool IsDue(DateTimeOffset now, DateTimeOffset? lastAttempt) =>
        !lastAttempt.HasValue || now - lastAttempt.Value >= TimeSpan.FromDays(1) || lastAttempt.Value - now > TimeSpan.FromMinutes(5);

    public static bool ShouldNotify(Settings settings, SoftwareRelease? release, bool busy) =>
        settings.AutoCheckSoftwareUpdates && release is not null && !busy && settings.LastNotifiedSoftwareVersion != release.Tag;
}
