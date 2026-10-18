using System.IO;
using System.Net.Http;
using Sukun.Core;

namespace Sukun.Windows;

public sealed class DiyanetClient : IDisposable
{
    private const string Host = "namazvakitleri.diyanet.gov.tr";
    private readonly HttpClient client = new(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
    { Timeout = TimeSpan.FromSeconds(25), MaxResponseContentBufferSize = 4_000_000 };

    public async Task<Timetable> DownloadAsync(string id, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id) || !id.All(char.IsAsciiDigit)) throw L.Data("InvalidPlaceId");
        var uri = new Uri($"https://{Host}/tr-TR/{id}");
        for (var redirects = 0; redirects < 4; redirects++)
        {
            using var response = await client.GetAsync(uri, cancellationToken);
            if ((int)response.StatusCode is >= 300 and < 400)
            {
                var location = response.Headers.Location ?? throw L.Data("MissingRedirect");
                uri = new Uri(uri, location);
                if (uri.Scheme != "https" || uri.Host != Host || !uri.IsDefaultPort || !string.IsNullOrEmpty(uri.UserInfo))
                    throw L.Data("UnsafeRedirect");
                continue;
            }
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            return await Task.Run(() => DiyanetParser.Parse(html, id, now), cancellationToken);
        }
        throw L.Data("TooManyRedirects");
    }
    public void Dispose() => client.Dispose();
}
