using Bewerbo.ViewModels;

namespace Bewerbo.Services;

public interface IDialogService
{
    void ShowEditor(EditorViewModel editor);

    void ShowSettings(SettingsViewModel settings);

    void ShowFolderImport(FolderImportViewModel import);

    void ShowFiles(FilesViewModel files);

    bool Confirm(string message, string title);

    void ShowInfo(string message, string title);

    void ShowError(string message, string title);

    string? PickExportFile(string suggestedName);

    string? PickImportFile();

    string? PickFolder(string title, string initialFolder);

    IReadOnlyList<string> PickFiles(string title);

    void OpenLink(string link);

    void OpenPath(string path);

    /// <returns>False when the file could not be moved; the user has been told why.</returns>
    bool MoveToRecycleBin(string path);
}
