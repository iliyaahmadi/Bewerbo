using System.Windows;
using System.Windows.Controls;
using Bewerbo.Core;
using Bewerbo.Services;
using Bewerbo.ViewModels;

namespace Bewerbo.Views;

public partial class EditorWindow : Window
{
    private readonly EditorViewModel _editor;

    public EditorWindow(EditorViewModel editor)
    {
        InitializeComponent();
        ThemeManager.Attach(this);
        DataContext = _editor = editor;
        editor.CloseRequested += (_, _) => Close();
        Loaded += (_, _) =>
        {
            editor.OfferClipboard(ClipboardText.Read());
            CompanyBox.Focus();
        };
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        var accepts = _editor.Documents is not null && e.Data.GetDataPresent(DataFormats.FileDrop);
        e.Effects = accepts ? DragDropEffects.Copy : DragDropEffects.None;
        if (accepts)
            DropZone.SetResourceReference(Border.BorderBrushProperty, "AccentBrush");
        e.Handled = true;
    }

    private void OnDragLeave(object sender, DragEventArgs e) =>
        DropZone.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");

    private void OnDrop(object sender, DragEventArgs e)
    {
        OnDragLeave(sender, e);
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
            _editor.Documents?.Add(files);
        e.Handled = true;
    }

    private void OnPasteClick(object sender, RoutedEventArgs e) => _editor.Paste(ClipboardText.Read());

    private void OnCopyPromptClick(object sender, RoutedEventArgs e)
    {
        if (ClipboardText.Write(DetailsParser.Prompt))
            CopyPromptButton.Content = "Prompt copied ✓";
    }
}
