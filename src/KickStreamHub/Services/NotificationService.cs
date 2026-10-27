using System.Windows.Forms;
using Application = System.Windows.Application;

namespace KickStreamHub.Services;

/// <summary>
/// Sistem tepsisi simgesini ve bildirimleri (balloon tip) yönetir.
/// </summary>
public sealed class NotificationService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;

    public event EventHandler? OpenRequested;
    public event EventHandler? ExitRequested;

    public NotificationService()
    {
        System.Drawing.Icon? customIcon = null;
        try
        {
            var streamInfo = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/app.ico"));
            if (streamInfo?.Stream != null)
            {
                customIcon = new System.Drawing.Icon(streamInfo.Stream);
            }
        }
        catch { }

        _notifyIcon = new NotifyIcon
        {
            Icon = customIcon ?? System.Drawing.SystemIcons.Application,
            Text = "LD KickWatch",
            Visible = false,
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("LD KickWatch'u Göster", null, (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Çıkış", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));
        _notifyIcon.ContextMenuStrip = menu;

        _notifyIcon.DoubleClick += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Show() => _notifyIcon.Visible = true;
    public void Hide() => _notifyIcon.Visible = false;

    public void ShowBalloon(string title, string text)
    {
        try
        {
            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = text;
            _notifyIcon.ShowBalloonTip(6000);
        }
        catch
        {
            // bildirim gösterilemezse sessizce yoksay
        }
    }

    public void Dispose()
    {
        try
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
        catch { }
    }
}
