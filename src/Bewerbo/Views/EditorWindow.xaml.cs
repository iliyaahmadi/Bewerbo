using System.Windows;
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

    private void OnPasteClick(object sender, RoutedEventArgs e) => _editor.Paste(ClipboardText.Read());

    private void OnCopyPromptClick(object sender, RoutedEventArgs e)
    {
        if (ClipboardText.Write(DetailsParser.Prompt))
            CopyPromptButton.Content = "Prompt copied ✓";
    }
}
