using System.Windows;
using System.Windows.Input;
using Bewerbo.Services;

namespace Bewerbo.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        WindowTheme.Attach(this);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
        }

        base.OnPreviewKeyDown(e);
    }
}
