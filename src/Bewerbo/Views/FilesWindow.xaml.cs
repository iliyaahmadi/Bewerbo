using System.Windows;
using Bewerbo.Services;
using Bewerbo.ViewModels;

namespace Bewerbo.Views;

public partial class FilesWindow : Window
{
    public FilesWindow(FilesViewModel files)
    {
        InitializeComponent();
        ThemeManager.Attach(this);
        DataContext = files;
    }
}
