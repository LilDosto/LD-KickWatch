using System.IO;
using System.Text.Json;
using KickStreamHub.Models;

namespace KickStreamHub.Services;

/// <summary>
/// Uygulama yapılandırmasını %LOCALAPPDATA%\LDKickWatch\config.json
/// dosyasında saklar. Eski KickStreamHub klasöründe yapılandırma varsa ilk
/// açılışta otomatik olarak taşır.
/// </summary>
public sealed class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public string AppDataDirectory { get; }
    public string ConfigFilePath { get; }
    public string AvatarCacheDirectory { get; }

    public ConfigService()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        AppDataDirectory = Path.Combine(localAppData, "LDKickWatch");
        Directory.CreateDirectory(AppDataDirectory);

        AvatarCacheDirectory = Path.Combine(AppDataDirectory, "avatars");
        Directory.CreateDirectory(AvatarCacheDirectory);

        ConfigFilePath = Path.Combine(AppDataDirectory, "config.json");

        // Eski klasörden (KickStreamHub) ayarları otomatik taşı
        if (!File.Exists(ConfigFilePath))
        {
            var legacyConfig = Path.Combine(localAppData, "KickStreamHub", "config.json");
            if (File.Exists(legacyConfig))
            {
                try { File.Copy(legacyConfig, ConfigFilePath, overwrite: false); } catch { }
            }
        }
    }

    public AppConfig Load()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
                return new AppConfig();

            var json = File.ReadAllText(ConfigFilePath);
            var config = JsonSerializer.Deserialize<AppConfig>(json);
            return config ?? new AppConfig();
        }
        catch (Exception ex)
        {
            LoggingService.Instance.Error("Yapılandırma okunamadı, varsayılana dönülüyor.", ex);
            return new AppConfig();
        }
    }

    public void Save(AppConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            LoggingService.Instance.Error("Yapılandırma kaydedilemedi.", ex);
        }
    }
}
