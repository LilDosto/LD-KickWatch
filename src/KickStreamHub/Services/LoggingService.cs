using System.IO;
using System.Text;

namespace KickStreamHub.Services;

public enum LogLevel { Info, Warning, Error }

/// <summary>
/// Basit, dosya tabanlı, iş parçacığı güvenli (thread-safe) log servisi.
/// %LOCALAPPDATA%\KickStreamHub\logs\app.log dosyasına yazar ve
/// belirli bir boyutu (5 MB) aştığında dosyayı döndürür (rotate).
/// </summary>
public sealed class LoggingService
{
    private static readonly Lazy<LoggingService> _instance = new(() => new LoggingService());
    public static LoggingService Instance => _instance.Value;

    private readonly string _logFilePath;
    private readonly object _lock = new();
    private const long MaxSizeBytes = 5 * 1024 * 1024;

    private LoggingService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LDKickWatch", "logs");
        Directory.CreateDirectory(dir);
        _logFilePath = Path.Combine(dir, "app.log");
    }

    public void Info(string message) => Write(LogLevel.Info, message);
    public void Warning(string message) => Write(LogLevel.Warning, message);
    public void Error(string message, Exception? ex = null) =>
        Write(LogLevel.Error, ex is null ? message : $"{message} :: {ex}");

    private void Write(LogLevel level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        lock (_lock)
        {
            try
            {
                RotateIfNeeded();
                File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // Loglama asla uygulamayı çökertmemeli.
            }
        }
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(_logFilePath)) return;
        var info = new FileInfo(_logFilePath);
        if (info.Length < MaxSizeBytes) return;
        var backup = Path.ChangeExtension(_logFilePath, ".1.log");
        try
        {
            if (File.Exists(backup)) File.Delete(backup);
            File.Move(_logFilePath, backup);
        }
        catch
        {
            // yoksay
        }
    }

    public string LogDirectory => Path.GetDirectoryName(_logFilePath)!;
}
