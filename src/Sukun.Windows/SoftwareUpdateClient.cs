using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Sukun.Core;

namespace Sukun.Windows;

public static class ReleaseSource
{
    public static Uri DownloadPage { get; } = new("https://talhay61.github.io/sukun/");
    public static GitHubRepository? Repository { get; } = GitHubRepository.Parse(typeof(ReleaseSource).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "Sukun.Repository")?.Value);
    public static bool IsPrerelease => UpdateVersion.Parse(VersionLabel)?.Prerelease is not null;
    public static Version CurrentVersion { get; } = typeof(ReleaseSource).Assembly.GetName().Version ?? new Version(0, 1, 0);
    public static string VersionLabel { get; } = (typeof(ReleaseSource).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.1").Split('+')[0];
}

public sealed class SoftwareUpdateClient : IDisposable
{
    private readonly HttpClient client;

    public SoftwareUpdateClient(HttpMessageHandler? handler = null)
    {
        client = new(handler ?? new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
        { Timeout = TimeSpan.FromSeconds(12), MaxResponseContentBufferSize = 1_000_000 };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Sukun");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2026-03-10");
    }

    public async Task<SoftwareRelease?> CheckAsync(GitHubRepository repository, string? currentVersion = null)
    {
        var current = currentVersion ?? ReleaseSource.VersionLabel;
        var version = UpdateVersion.Parse(current) ?? throw L.Data("UnknownVersion");
        var endpoint = version.Prerelease is null ? repository.LatestReleaseApi : repository.ReleasesApi;
        using var response = await client.GetAsync(endpoint);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        if ((int)response.StatusCode is 403 or 429) throw L.Data("GitHubRateLimit");
        response.EnsureSuccessStatusCode();
        return SoftwareRelease.FindUpdate(await response.Content.ReadAsStringAsync(), repository, current);
    }

    public void Dispose() => client.Dispose();
}
