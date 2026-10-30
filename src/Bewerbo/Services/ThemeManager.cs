using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Bewerbo.Core;
using Microsoft.Win32;

namespace Bewerbo.Services;

/// <summary>
/// Switches between the light and dark palettes and keeps the Windows title bar in step.
/// In System mode both follow the Windows app theme, including changes while the app is open.
/// </summary>
public static partial class ThemeManager
{
    private const int UseImmersiveDarkMode = 20;
    private const int UseImmersiveDarkModeBefore20H1 = 19;
    private const int WindowCornerPreference = 33;
    private const int RoundCorners = 2;

    private static readonly Uri DarkPalette = new("pack://application:,,,/Bewerbo;component/Themes/Dark.xaml");
    private static readonly Uri LightPalette = new("pack://application:,,,/Bewerbo;component/Themes/Light.xaml");

    private static bool _watchingSystem;

    public static AppTheme Mode { get; private set; }

    public static bool IsDark => Mode switch
    {
        AppTheme.Dark => true,
        AppTheme.Light => false,
        _ => SystemUsesDarkMode(),
    };

    public static void Apply(Application application, AppTheme mode)
    {
        Mode = mode;
        WatchSystemTheme(application);

        var palettes = application.Resources.MergedDictionaries;
        var palette = new ResourceDictionary { Source = IsDark ? DarkPalette : LightPalette };
        if (palettes.Count > 0 && IsPalette(palettes[0]))
            palettes[0] = palette;
        else
            palettes.Insert(0, palette);

        foreach (Window window in application.Windows)
            ApplyTitleBar(window);
    }

    public static void Attach(Window window) =>
        window.SourceInitialized += (_, _) => ApplyTitleBar(window);

    private static bool IsPalette(ResourceDictionary dictionary) =>
        dictionary.Source?.OriginalString.EndsWith("Dark.xaml", StringComparison.OrdinalIgnoreCase) == true
        || dictionary.Source?.OriginalString.EndsWith("Light.xaml", StringComparison.OrdinalIgnoreCase) == true;

    private static void WatchSystemTheme(Application application)
    {
        if (_watchingSystem)
            return;

        _watchingSystem = true;
        SystemEvents.UserPreferenceChanged += (_, e) =>
        {
            if (e.Category == UserPreferenceCategory.General && Mode == AppTheme.System)
                application.Dispatcher.InvokeAsync(() => Apply(application, AppTheme.System));
        };
    }

    private static void ApplyTitleBar(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
            return;

        var dark = IsDark ? 1 : 0;
        if (DwmSetWindowAttribute(handle, UseImmersiveDarkMode, ref dark, sizeof(int)) != 0)
            DwmSetWindowAttribute(handle, UseImmersiveDarkModeBefore20H1, ref dark, sizeof(int));

        // Windows 11 does not round windows that draw their own title bar unless asked to. Older versions ignore this.
        var corners = RoundCorners;
        DwmSetWindowAttribute(handle, WindowCornerPreference, ref corners, sizeof(int));
    }

    private static bool SystemUsesDarkMode()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
    }

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
