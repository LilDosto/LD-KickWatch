using System.Threading;
using System.Windows;
using System.Windows.Threading;
using KickStreamHub.Services;
using KickStreamHub.Views;

namespace KickStreamHub;

public partial class App : System.Windows.Application
{
    private const string MutexName = @"Local\LDKickWatch_SingleInstance_Mutex";
    private const string ActivateEventName = @"Local\LDKickWatch_Activate_Event";

    private static Mutex? _singleInstanceMutex;
    private static EventWaitHandle? _activateEvent;
    private static bool _ownsMutex;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstanceMutex = new Mutex(true, MutexName, out bool isNewInstance);
        _ownsMutex = isNewInstance;

        if (!isNewInstance)
        {
            // Çalışmakta olan örneği öne getir
            try
            {
                using var evt = EventWaitHandle.OpenExisting(ActivateEventName);
                evt.Set();
            }
            catch { }

            System.Windows.MessageBox.Show(
                "LD KickWatch zaten çalışıyor! Açık olan pencere ekrana getirildi.",
                "LD KickWatch",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Environment.Exit(0);
            return;
        }

        ShutdownMode = ShutdownMode.OnMainWindowClose;

        // İkinci bir örnek açılmaya çalışıldığında ana pencereyi öne getiren dinleyici
        _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateEventName);
        ThreadPool.RegisterWaitForSingleObject(_activateEvent, (_, _) =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (_mainWindow != null)
                {
                    _mainWindow.RestoreAndBringToFront();
                }
            });
        }, null, -1, false);

        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _mainWindow = new MainWindow();
        MainWindow = _mainWindow;
        _mainWindow.Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LoggingService.Instance.Error("Yakalanmayan UI hatası", e.Exception);
        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            LoggingService.Instance.Error("Yakalanmayan genel hata", ex);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LoggingService.Instance.Error("Gözlemlenmeyen görev (task) hatası", e.Exception);
        e.SetObserved();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { _activateEvent?.Dispose(); } catch { }
        if (_ownsMutex)
        {
            try { _singleInstanceMutex?.ReleaseMutex(); } catch { }
        }
        try { _singleInstanceMutex?.Dispose(); } catch { }
        base.OnExit(e);
    }
}
