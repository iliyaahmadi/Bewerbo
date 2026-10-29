using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;

namespace Bewerbo.Services;

/// <summary>Makes the Windows title bar follow the system light/dark setting, including live changes.</summary>
public static partial class WindowTheme
{
    private const int UseImmersiveDarkMode = 20;
    private const int UseImmersiveDarkModeBefore20H1 = 19;

    static WindowTheme()
    {
        SystemEvents.UserPreferenceChanged += (_, _) =>
            Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                foreach (Window window in Application.Current.Windows)
                    Apply(window);
            });
    }

    public static void Attach(Window window) =>
        window.SourceInitialized += (_, _) => Apply(window);

    private static void Apply(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
            return;

        var dark = SystemUsesDarkMode() ? 1 : 0;
        if (DwmSetWindowAttribute(handle, UseImmersiveDarkMode, ref dark, sizeof(int)) != 0)
            DwmSetWindowAttribute(handle, UseImmersiveDarkModeBefore20H1, ref dark, sizeof(int));
    }

    private static bool SystemUsesDarkMode()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
    }

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
