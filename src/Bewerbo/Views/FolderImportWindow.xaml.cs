using System.Windows;
using Bewerbo.Services;
using Bewerbo.ViewModels;

namespace Bewerbo.Views;

public partial class FolderImportWindow : Window
{
    public FolderImportWindow(FolderImportViewModel import)
    {
        InitializeComponent();
        ThemeManager.Attach(this);
        DataContext = import;
        import.CloseRequested += (_, _) => Close();
    }
}
