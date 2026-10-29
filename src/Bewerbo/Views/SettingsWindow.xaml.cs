using System.Windows;
using Bewerbo.Services;
using Bewerbo.ViewModels;

namespace Bewerbo.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel settings)
    {
        InitializeComponent();
        ThemeManager.Attach(this);
        DataContext = settings;
        settings.CloseRequested += (_, _) => Close();
    }
}
