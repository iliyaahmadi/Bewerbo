using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Bewerbo.Core;
using Bewerbo.ViewModels;
using Bewerbo.Views;
using Microsoft.Win32;

namespace Bewerbo.Services;

public sealed class DialogService(Window owner) : IDialogService
{
    private const string BackupFilter = "Bewerbo backup (*.json)|*.json";

    public void ShowEditor(EditorViewModel editor) =>
        new EditorWindow(editor) { Owner = ActiveWindow }.ShowDialog();

    public void ShowSettings(SettingsViewModel settings) =>
        new SettingsWindow(settings) { Owner = owner }.ShowDialog();

    public void ShowFolderImport(FolderImportViewModel import) =>
        new FolderImportWindow(import) { Owner = owner }.ShowDialog();

    public void ShowFiles(FilesViewModel files) =>
        new FilesWindow(files) { Owner = ActiveWindow }.ShowDialog();

    public void ShowFilters(FiltersViewModel filters) =>
        new FiltersWindow(filters) { Owner = owner }.ShowDialog();

    public bool Confirm(string message, string title) =>
        MessageBox.Show(ActiveWindow, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public void ShowInfo(string message, string title) =>
        MessageBox.Show(ActiveWindow, message, title);

    public void ShowError(string message, string title) =>
        MessageBox.Show(ActiveWindow, message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

    public string? PickExportFile(string suggestedName)
    {
        var dialog = new SaveFileDialog { Title = "Export your applications", Filter = BackupFilter, FileName = suggestedName };
        return dialog.ShowDialog(ActiveWindow) == true ? dialog.FileName : null;
    }

    public string? PickImportFile()
    {
        var dialog = new OpenFileDialog { Title = "Import a Bewerbo backup", Filter = BackupFilter };
        return dialog.ShowDialog(ActiveWindow) == true ? dialog.FileName : null;
    }

    public string? PickFolder(string title, string initialFolder)
    {
        var dialog = new OpenFolderDialog { Title = title };
        if (Directory.Exists(initialFolder))
            dialog.InitialDirectory = initialFolder;
        return dialog.ShowDialog(ActiveWindow) == true ? dialog.FolderName : null;
    }

    public IReadOnlyList<string> PickFiles(string title)
    {
        var dialog = new OpenFileDialog { Title = title, Multiselect = true };
        return dialog.ShowDialog(ActiveWindow) == true ? dialog.FileNames : [];
    }

    public void OpenLink(string link)
    {
        if (!WebLink.IsValid(link))
        {
            ShowError("Edit this application and enter a valid https:// or http:// link.", "Invalid link");
            return;
        }

        Start(link);
    }

    public void OpenPath(string path) => Start(path);

    public bool MoveToRecycleBin(string path)
    {
        try
        {
            RecycleBin.MoveFile(path);
            return true;
        }
        catch (IOException ex)
        {
            ShowError(ex.Message, "Unable to remove file");
            return false;
        }
    }

    private void Start(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            ShowError(ex.Message, "Unable to open");
        }
    }

    private Window ActiveWindow =>
        Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? owner;
}
