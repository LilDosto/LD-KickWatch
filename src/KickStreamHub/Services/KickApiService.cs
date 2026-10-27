using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using KickStreamHub.Models;

namespace KickStreamHub.Services;

/// <summary>
/// Kick.com genel API'siyle konuşan servis. .NET'in HttpClient'ı, Python
/// tarafında yaşanan curl_cffi kütüphanesinin aksine, birden fazla iş
/// parçacığından EŞZAMANLI olarak güvenle kullanılabilir; bu, eski Python
/// sürümünde uygulamanın rastgele çökmesine (access violation) yol açan
/// kök nedeni tamamen ortadan kaldırır.
/// </summary>
public sealed class KickApiService
{
    private const string ApiBaseV2 = "https://kick.com/api/v2/channels";
    private const string ApiBaseV1 = "https://kick.com/api/v1/channels";

    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            AutomaticDecompression = System.Net.DecompressionMethods.All,
            UseProxy = false,
        };
        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(12),
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
        return client;
    }

    public async Task<KickChannelResponse?> GetChannelAsync(string channelName, CancellationToken ct = default)
    {
        foreach (var baseUrl in new[] { ApiBaseV2, ApiBaseV1 })
        {
            try
            {
                using var resp = await Http.GetAsync($"{baseUrl}/{channelName}", ct).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode)
                    continue;

                var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                var data = await JsonSerializer.DeserializeAsync<KickChannelResponse>(stream, cancellationToken: ct)
                    .ConfigureAwait(false);
                if (data != null)
                    return data;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LoggingService.Instance.Warning($"Kanal bilgisi alınamadı ({channelName}, {baseUrl}): {ex.Message}");
            }
        }
        return null;
    }

    public async Task<byte[]?> GetImageBytesAsync(string url, CancellationToken ct = default)
    {
        try
        {
            using var resp = await Http.GetAsync(url, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LoggingService.Instance.Warning($"Görsel indirilemedi ({url}): {ex.Message}");
            return null;
        }
    }
}
