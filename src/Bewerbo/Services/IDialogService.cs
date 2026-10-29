using Bewerbo.ViewModels;

namespace Bewerbo.Services;

public interface IDialogService
{
    void ShowEditor(EditorViewModel editor);

    bool Confirm(string message, string title);

    void ShowInfo(string message, string title);

    void ShowError(string message, string title);

    string? PickExportFile(string suggestedName);

    string? PickImportFile();

    void OpenLink(string link);
}
