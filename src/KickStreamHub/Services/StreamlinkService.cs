using System.Diagnostics;
using System.IO;

namespace KickStreamHub.Services;

/// <summary>
/// Streamlink + oynatıcı (Pencere İçi VLC, Harici VLC, Harici MPV) işlemlerini bulan, başlatan ve
/// izleyen servis.
/// </summary>
public sealed class StreamlinkService
{
    private readonly Dictionary<string, Process> _activeProcesses = new(StringComparer.OrdinalIgnoreCase);
    private Process? _embeddedVlcProcess;
    private string? _embeddedVlcChannel;
    private readonly object _lock = new();

    public event EventHandler<string>? ProcessExited;

    public string? FindStreamlink(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
            return configuredPath;

        var candidates = new List<string>
        {
            "streamlink.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Streamlink", "bin", "streamlink.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Streamlink", "bin", "streamlink.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Streamlink", "bin", "streamlink.exe"),
        };

        foreach (var candidate in candidates)
        {
            var resolved = ResolveFromPathOrFile(candidate);
            if (resolved != null) return resolved;
        }
        return null;
    }

    public string? FindPlayer(string playerChoice, string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
            return configuredPath;

        bool isMpv = playerChoice.Equals("mpv", StringComparison.OrdinalIgnoreCase);
        var names = isMpv ? new[] { "mpv.exe" } : new[] { "vlc.exe" };

        var programFiles = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
        };

        foreach (var name in names)
        {
            var resolved = ResolveFromPathOrFile(name);
            if (resolved != null) return resolved;

            foreach (var pf in programFiles)
            {
                var candidate = Path.Combine(pf, isMpv ? "mpv" : "VideoLAN\\VLC", name);
                if (File.Exists(candidate)) return candidate;
            }
        }
        return null;
    }

    private static string? ResolveFromPathOrFile(string nameOrPath)
    {
        if (File.Exists(nameOrPath)) return Path.GetFullPath(nameOrPath);

        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var dir in pathEnv.Split(Path.PathSeparator))
        {
            try
            {
                var full = Path.Combine(dir, nameOrPath);
                if (File.Exists(full)) return full;
            }
            catch
            {
                // geçersiz path parçalarını yoksay
            }
        }
        return null;
    }

    public bool IsPlaying(string channelName)
    {
        lock (_lock)
        {
            if (_embeddedVlcProcess is { HasExited: false } &&
                string.Equals(_embeddedVlcChannel, channelName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (_activeProcesses.TryGetValue(channelName, out var proc))
            {
                if (!proc.HasExited) return true;
                _activeProcesses.Remove(channelName);
            }
            return false;
        }
    }

    /// <summary>
    /// VLC'yi doğrudan uygulama penceresi içindeki WinForms Panel HWND'sine (--drawable-hwnd) gömer.
    /// Streamlink kuruluysa Streamlink üzerinden, kurulu değilse Kick HLS playback_url üzerinden başlatır.
    /// </summary>
    public (bool success, string message) PlayEmbeddedVlc(
        string channelName,
        IntPtr hwnd,
        string? streamlinkPath,
        string vlcPath,
        string quality,
        string? playbackUrl,
        string? extraArgs = null)
    {
        StopEmbeddedVlc();

        try
        {
            ProcessStartInfo psi;
            string vlcEmbedFlags = $"-I dummy --dummy-quiet --drawable-hwnd={hwnd.ToInt64()} --no-video-title-show --mouse-hide-timeout=1000";

            if (!string.IsNullOrWhiteSpace(streamlinkPath) && File.Exists(streamlinkPath))
            {
                var url = $"https://kick.com/{channelName}";
                var args = $"\"{url}\" {quality} --player \"{vlcPath}\" --player-args \"{vlcEmbedFlags}\"";
                if (!string.IsNullOrWhiteSpace(extraArgs))
                    args += $" {extraArgs.Trim()}";

                psi = new ProcessStartInfo
                {
                    FileName = streamlinkPath,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                };
            }
            else if (!string.IsNullOrWhiteSpace(playbackUrl))
            {
                psi = new ProcessStartInfo
                {
                    FileName = vlcPath,
                    Arguments = $"\"{playbackUrl}\" {vlcEmbedFlags}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                };
            }
            else
            {
                return (false, "⚠ Pencere içi VLC için Streamlink kurulu olmalı veya kanal canlı yayında olmalıdır.");
            }

            var process = Process.Start(psi);
            if (process is null) return (false, "Pencere içi VLC başlatılamadı.");

            process.EnableRaisingEvents = true;
            process.Exited += (_, _) =>
            {
                lock (_lock)
                {
                    if (ReferenceEquals(_embeddedVlcProcess, process))
                    {
                        _embeddedVlcProcess = null;
                        _embeddedVlcChannel = null;
                    }
                }
                ProcessExited?.Invoke(this, channelName);
            };

            lock (_lock)
            {
                _embeddedVlcProcess = process;
                _embeddedVlcChannel = channelName;
            }

            return (true, $"🎬 {channelName.ToUpperInvariant()} yayını Pencere İçi VLC oynatıcıda başlatıldı.");
        }
        catch (Exception ex)
        {
            LoggingService.Instance.Error($"Pencere içi VLC başlatılamadı ({channelName})", ex);
            return (false, $"VLC Hatası: {ex.Message}");
        }
    }

    public void StopEmbeddedVlc()
    {
        lock (_lock)
        {
            if (_embeddedVlcProcess != null)
            {
                try { if (!_embeddedVlcProcess.HasExited) _embeddedVlcProcess.Kill(true); } catch { }
                _embeddedVlcProcess = null;
                _embeddedVlcChannel = null;
            }
        }
    }

    public (bool success, string message) Play(string channelName, string streamlinkPath, string playerPath, string quality, string? extraArgs = null)
    {
        lock (_lock)
        {
            if (_activeProcesses.TryGetValue(channelName, out var existing) && !existing.HasExited)
            {
                try { existing.Kill(true); } catch { /* yoksay */ }
                _activeProcesses.Remove(channelName);
                return (true, "Yayın durduruldu.");
            }
        }

        try
        {
            var url = $"https://kick.com/{channelName}";
            var args = $"\"{url}\" {quality} --player \"{playerPath}\" --title \"{channelName} - LD KickWatch\"";
            if (!string.IsNullOrWhiteSpace(extraArgs))
            {
                args += $" {extraArgs.Trim()}";
            }

            var psi = new ProcessStartInfo
            {
                FileName = streamlinkPath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            var process = Process.Start(psi);
            if (process is null) return (false, "İşlem başlatılamadı.");

            process.EnableRaisingEvents = true;
            process.Exited += (_, _) =>
            {
                lock (_lock)
                {
                    _activeProcesses.Remove(channelName);
                }
                ProcessExited?.Invoke(this, channelName);
            };

            lock (_lock)
            {
                _activeProcesses[channelName] = process;
            }
            return (true, $"{channelName.ToUpperInvariant()} yayını harici oynatıcıda başlatılıyor...");
        }
        catch (Exception ex)
        {
            LoggingService.Instance.Error($"Yayın başlatılamadı ({channelName})", ex);
            return (false, $"Hata: {ex.Message}");
        }
    }

    public void Stop(string channelName)
    {
        lock (_lock)
        {
            if (string.Equals(_embeddedVlcChannel, channelName, StringComparison.OrdinalIgnoreCase))
            {
                StopEmbeddedVlc();
            }

            if (_activeProcesses.TryGetValue(channelName, out var proc))
            {
                try { if (!proc.HasExited) proc.Kill(true); } catch { }
                _activeProcesses.Remove(channelName);
            }
        }
    }

    public void StopAll()
    {
        StopEmbeddedVlc();
        lock (_lock)
        {
            foreach (var proc in _activeProcesses.Values)
            {
                try { if (!proc.HasExited) proc.Kill(true); } catch { /* yoksay */ }
            }
            _activeProcesses.Clear();
        }
    }
}
