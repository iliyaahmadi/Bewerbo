using System.Windows;
using System.Windows.Controls;
using Bewerbo.ViewModels;

namespace Bewerbo.Views;

public partial class DocumentsView : UserControl
{
    public DocumentsView() => InitializeComponent();

    private void OnDragOver(object sender, DragEventArgs e)
    {
        var accepts = DataContext is DocumentsViewModel && e.Data.GetDataPresent(DataFormats.FileDrop);
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
        if (DataContext is DocumentsViewModel documents && e.Data.GetData(DataFormats.FileDrop) is string[] files)
            documents.Add(files);
        e.Handled = true;
    }
}
