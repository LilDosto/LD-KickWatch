using System.IO;

namespace KickStreamHub.Services;

/// <summary>
/// Kanal avatarlarını diskte önbelleğe alır (TTL: 24 saat) ve gerektiğinde
/// KickApiService üzerinden indirir. Tüm dosya G/Ç işlemleri iş parçacığı
/// güvenlidir; her indirme kendi dosya adını kullandığından çakışma olmaz.
/// </summary>
public sealed class AvatarCacheService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    private readonly string _cacheDir;
    private readonly KickApiService _api;

    public AvatarCacheService(string cacheDir, KickApiService api)
    {
        _cacheDir = cacheDir;
        _api = api;
    }

    public string GetCachePath(string channelName) => Path.Combine(_cacheDir, $"{Sanitize(channelName)}.png");

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    public bool TryGetFreshCachedPath(string channelName, out string path)
    {
        path = GetCachePath(channelName);
        if (!File.Exists(path)) return false;
        var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(path);
        return age < Ttl;
    }

    public async Task<string?> DownloadAndCacheAsync(string channelName, string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var bytes = await _api.GetImageBytesAsync(url, ct).ConfigureAwait(false);
        if (bytes is null || bytes.Length == 0) return null;

        var path = GetCachePath(channelName);
        try
        {
            var tmp = path + ".tmp";
            await File.WriteAllBytesAsync(tmp, bytes, ct).ConfigureAwait(false);
            File.Move(tmp, path, overwrite: true);
            return path;
        }
        catch (Exception ex)
        {
            LoggingService.Instance.Warning($"Avatar önbelleğe yazılamadı ({channelName}): {ex.Message}");
            return null;
        }
    }

    public void CleanupStale(IEnumerable<string> activeChannels)
    {
        try
        {
            var activeSet = new HashSet<string>(activeChannels.Select(Sanitize), StringComparer.OrdinalIgnoreCase);
            foreach (var file in Directory.EnumerateFiles(_cacheDir, "*.png"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(file);
                if (!activeSet.Contains(name) && age > TimeSpan.FromDays(7))
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception ex)
        {
            LoggingService.Instance.Warning($"Avatar önbellek temizliği başarısız: {ex.Message}");
        }
    }
}
