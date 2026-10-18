using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Sukun.Core;

namespace Sukun.Windows;

public static class ReleaseSource
{
    public static GitHubRepository? Repository { get; } = GitHubRepository.Parse(typeof(ReleaseSource).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "Sukun.Repository")?.Value);
    public static bool IsPrerelease => VersionLabel.Contains('-');
    public static Version CurrentVersion { get; } = typeof(ReleaseSource).Assembly.GetName().Version ?? new Version(0, 1, 0);
    public static string VersionLabel { get; } = (typeof(ReleaseSource).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.1.0-beta.2").Split('+')[0];
}

public sealed class SoftwareUpdateClient : IDisposable
{
    private readonly HttpClient client = new(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
    { Timeout = TimeSpan.FromSeconds(12), MaxResponseContentBufferSize = 1_000_000 };

    public SoftwareUpdateClient()
    {
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Sukun");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2026-03-10");
    }

    public async Task<SoftwareRelease?> CheckAsync(GitHubRepository repository)
    {
        using var response = await client.GetAsync(repository.LatestReleaseApi);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw L.Data("NoStableRelease");
        if ((int)response.StatusCode is 403 or 429)
            throw L.Data("GitHubRateLimit");
        response.EnsureSuccessStatusCode();
        var release = SoftwareRelease.Parse(await response.Content.ReadAsStringAsync(), repository);
        return release.IsNewerThan(ReleaseSource.CurrentVersion, ReleaseSource.IsPrerelease) ? release : null;
    }
    public void Dispose() => client.Dispose();
}
