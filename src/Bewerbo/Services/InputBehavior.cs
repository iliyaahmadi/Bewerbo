using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Bewerbo.Services;

/// <summary>Small fixes to how WPF handles the mouse, applied to every window and dropdown in the app.</summary>
public static class InputBehavior
{
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
            return;

        _registered = true;
        EventManager.RegisterClassHandler(typeof(ComboBox), UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnComboBoxMouseWheel));
        EventManager.RegisterClassHandler(typeof(Window), UIElement.MouseDownEvent, new MouseButtonEventHandler(OnWindowMouseDown), handledEventsToo: true);
    }

    /// <summary>
    /// A closed dropdown changes its selection when the mouse wheel turns over it. Scroll the page instead,
    /// so scrolling through the cards never changes a status or the sort order by accident.
    /// </summary>
    private static void OnComboBoxMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var comboBox = (ComboBox)sender;
        if (comboBox.IsDropDownOpen)
            return;

        e.Handled = true;
        if (VisualTreeHelper.GetParent(comboBox) is UIElement parent)
            parent.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta) { RoutedEvent = UIElement.MouseWheelEvent, Source = comboBox });
    }

    /// <summary>Clicking empty space takes the focus away from the text field or button that had it.</summary>
    private static void OnWindowMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Window window || IsInsideControl(e.OriginalSource as DependencyObject))
            return;

        Keyboard.Focus(window);
    }

    private static bool IsInsideControl(DependencyObject? element)
    {
        for (; element is not null; element = element is Visual ? VisualTreeHelper.GetParent(element) : LogicalTreeHelper.GetParent(element))
        {
            if (element is TextBoxBase or ComboBox or ButtonBase or ScrollBar or ListBoxItem)
                return true;
        }

        return false;
    }
}
