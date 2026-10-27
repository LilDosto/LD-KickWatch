using System.IO;
using System.Media;

namespace KickStreamHub.Services;

/// <summary>
/// Canlı yayın başlangıcı, kategori değişikliği ve anahtar kelime uyarıları için
/// sesli bildirim çalar. Özel .wav dosyası seçilmemişse bellekte yumuşak çift tonlu
/// bir bildirim sesi (chime) üretip çalar.
/// </summary>
public static class SoundService
{
    private static readonly byte[] DefaultChimeWav = GeneratePleasantChimeWav(587.33, 880.0, 260); // D5 -> A5
    private static readonly byte[] KeywordAlertWav = GeneratePleasantChimeWav(659.25, 1046.50, 300); // E5 -> C6

    public static void PlayNotification(string? customSoundPath = null, bool isKeywordOrCategoryAlert = false)
    {
        Task.Run(() =>
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(customSoundPath) && File.Exists(customSoundPath))
                {
                    using var player = new SoundPlayer(customSoundPath);
                    player.PlaySync();
                    return;
                }

                var wavBytes = isKeywordOrCategoryAlert ? KeywordAlertWav : DefaultChimeWav;
                using var ms = new MemoryStream(wavBytes);
                using var chimePlayer = new SoundPlayer(ms);
                chimePlayer.PlaySync();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Warning($"Bildirim sesi çalınamadı: {ex.Message}");
            }
        });
    }

    private static byte[] GeneratePleasantChimeWav(double freq1, double freq2, int durationMs)
    {
        const int sampleRate = 44100;
        int totalSamples = sampleRate * durationMs / 1000;
        int halfSamples = totalSamples / 2;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        int dataSize = totalSamples * 2;
        bw.Write("RIFF"u8.ToArray());
        bw.Write(36 + dataSize);
        bw.Write("WAVE"u8.ToArray());
        bw.Write("fmt "u8.ToArray());
        bw.Write(16);
        bw.Write((short)1); // PCM
        bw.Write((short)1); // Mono
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2);
        bw.Write((short)2);
        bw.Write((short)16);
        bw.Write("data"u8.ToArray());
        bw.Write(dataSize);

        for (int i = 0; i < totalSamples; i++)
        {
            double freq = i < halfSamples ? freq1 : freq2;
            int localIndex = i < halfSamples ? i : i - halfSamples;
            int localLen = i < halfSamples ? halfSamples : totalSamples - halfSamples;

            double t = (double)localIndex / sampleRate;
            double envelope = Math.Exp(-4.5 * localIndex / localLen);
            if (localIndex < 200) envelope *= localIndex / 200.0;

            double sample = Math.Sin(2 * Math.PI * freq * t) * 0.35 * envelope;
            short pcm = (short)(sample * short.MaxValue);
            bw.Write(pcm);
        }

        return ms.ToArray();
    }
}
