using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace Bewerbo.Services;

/// <summary>
/// Support for the app's own title bar (the AppWindow style): the minimize, maximize and close buttons,
/// and the padding a maximized window needs so its edges stay on screen.
/// </summary>
public static partial class WindowFrame
{
    private const int SizeFrame = 32;
    private const int PaddedBorder = 92;

    private static bool _registered;

    /// <summary>
    /// A maximized window without the standard frame still extends past the screen by the width of the
    /// resize border, so its content is padded by that much.
    /// </summary>
    public static Thickness MaximizedPadding { get; } = ComputeMaximizedPadding();

    public static void Register()
    {
        if (_registered)
            return;

        _registered = true;
        Bind(SystemCommands.MinimizeWindowCommand, SystemCommands.MinimizeWindow);
        Bind(SystemCommands.MaximizeWindowCommand, SystemCommands.MaximizeWindow);
        Bind(SystemCommands.RestoreWindowCommand, SystemCommands.RestoreWindow);
        Bind(SystemCommands.CloseWindowCommand, SystemCommands.CloseWindow);
    }

    private static void Bind(RoutedCommand command, Action<Window> action) =>
        CommandManager.RegisterClassCommandBinding(typeof(Window),
            new CommandBinding(command, (sender, _) => action((Window)sender), (_, e) => e.CanExecute = true));

    private static Thickness ComputeMaximizedPadding()
    {
        try
        {
            var scale = GetDpiForSystem() / 96.0;
            var border = (GetSystemMetrics(SizeFrame) + GetSystemMetrics(PaddedBorder)) / scale;
            return border > 0 ? new Thickness(border) : new Thickness(7);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            return new Thickness(7);
        }
    }

    [LibraryImport("user32.dll")]
    private static partial int GetSystemMetrics(int index);

    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForSystem();
}
