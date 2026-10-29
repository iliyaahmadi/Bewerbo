using System.Windows;
using Bewerbo.Services;
using Bewerbo.ViewModels;

namespace Bewerbo.Views;

public partial class FiltersWindow : Window
{
    public FiltersWindow(FiltersViewModel filters)
    {
        InitializeComponent();
        ThemeManager.Attach(this);
        DataContext = filters;
        filters.CloseRequested += (_, _) => Close();
    }
}
