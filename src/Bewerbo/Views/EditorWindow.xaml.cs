using System.Runtime.InteropServices;
using System.Windows;
using Bewerbo.Services;
using Bewerbo.ViewModels;

namespace Bewerbo.Views;

public partial class EditorWindow : Window
{
    private readonly EditorViewModel _editor;

    public EditorWindow(EditorViewModel editor)
    {
        InitializeComponent();
        WindowTheme.Attach(this);
        DataContext = _editor = editor;
        editor.CloseRequested += (_, _) => Close();
        Loaded += (_, _) => CompanyBox.Focus();
    }

    private void OnPasteClick(object sender, RoutedEventArgs e)
    {
        string? text = null;
        try
        {
            if (Clipboard.ContainsText())
                text = Clipboard.GetText();
        }
        catch (COMException)
        {
            // Another app is holding the clipboard; treat it as empty.
        }

        _editor.Paste(text);
    }
}
