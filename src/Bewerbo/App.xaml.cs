using System.IO;
using System.Windows;
using System.Windows.Threading;
using Bewerbo.Core.Storage;
using Bewerbo.Services;
using Bewerbo.ViewModels;

namespace Bewerbo;

public partial class App : Application
{
    private Mutex? _singleInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new Mutex(initiallyOwned: true, @"Local\Bewerbo", out var isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show("Bewerbo is already open. Check your taskbar.", "Bewerbo");
            Shutdown();
            return;
        }

        DispatcherUnhandledException += OnUnhandledException;
        InputBehavior.Register();
        WindowFrame.Register();

        try
        {
            DataFolder.CopyLegacyData(DataFolder.Default, DataFolder.Legacy);
            var store = new ApplicationStore(Path.Combine(DataFolder.Default, DataFolder.FileName));
            var settings = new SettingsStore(Path.Combine(DataFolder.Default, SettingsStore.FileName));
            var favicons = new FaviconCache(Path.Combine(store.DataFolder, "icons"));
            ThemeManager.Apply(this, settings.Load().Theme);

            var window = new Views.MainWindow();
            window.DataContext = new MainViewModel(store, settings, favicons, new DialogService(window), theme => ThemeManager.Apply(this, theme));
            MainWindow = window;
            window.Show();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            MessageBox.Show(
                $"Bewerbo could not open. No saved applications have been changed.\n\n{ex.Message}\n\nYour data is in: {DataFolder.Default}",
                "Bewerbo", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstance?.Dispose();
        base.OnExit(e);
    }

    private static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            "Something went wrong. Your saved applications are safe.\n\n" + e.Exception.Message,
            "Bewerbo", MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true;
    }
}
