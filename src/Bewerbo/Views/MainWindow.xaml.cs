using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Bewerbo.Services;
using Bewerbo.ViewModels;

namespace Bewerbo.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ThemeManager.Attach(this);

        // Files may have been added or removed in Explorer while the app was in the background.
        Activated += (_, _) => (DataContext as MainViewModel)?.RefreshFiles();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
        }
        else if (e.Key == Key.OemComma && Keyboard.Modifiers == ModifierKeys.Control && DataContext is MainViewModel settings)
        {
            settings.SettingsCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control
            && Keyboard.FocusedElement is not TextBox
            && DataContext is MainViewModel main)
        {
            // Pasting copied application details anywhere outside a text field starts a new application.
            e.Handled = main.AddFromText(ClipboardText.Read());
        }

        base.OnPreviewKeyDown(e);
    }

    private void OnCardDragOver(object sender, DragEventArgs e)
    {
        var hasFiles = e.Data.GetDataPresent(DataFormats.FileDrop);
        e.Effects = hasFiles ? DragDropEffects.Copy : DragDropEffects.None;
        if (hasFiles && sender is Border card)
            card.SetResourceReference(Border.BorderBrushProperty, "AccentBrush");
        e.Handled = true;
    }

    private void OnCardDragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border card)
            card.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
    }

    private void OnCardDrop(object sender, DragEventArgs e)
    {
        OnCardDragLeave(sender, e);
        if (sender is Border { DataContext: ApplicationCardViewModel card } && e.Data.GetData(DataFormats.FileDrop) is string[] files)
        {
            // Let the drag source finish before a dialog can open.
            Dispatcher.InvokeAsync(() => card.AddFiles(files));
            e.Handled = true;
        }
    }
}
