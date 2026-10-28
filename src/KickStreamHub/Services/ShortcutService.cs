using System.Diagnostics;
using System.IO;

namespace KickStreamHub.Services;

public static class ShortcutService
{
    public static bool CreateDesktopShortcut(out string message)
    {
        try
        {
            var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                message = "Uygulama yürütülebilir dosyası bulunamadı.";
                return false;
            }

            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
            {
                message = "Windows Script Shell servisine ulaşılamadı.";
                return false;
            }

            dynamic? shell = Activator.CreateInstance(shellType);
            if (shell == null)
            {
                message = "Kısayol oluşturucu başlatılamadı.";
                return false;
            }

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string shortcutPath = Path.Combine(desktopPath, "LD KickWatch.lnk");

            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = exePath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(exePath) ?? "";
            shortcut.Description = "LD KickWatch - Kick.com Canlı Yayın İzleme Stüdyosu";
            shortcut.IconLocation = $"{exePath},0";
            shortcut.Save();

            message = "Masaüstü kısayolu başarıyla oluşturuldu!";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Kısayol oluşturulurken hata oluştu: {ex.Message}";
            return false;
        }
    }
}
