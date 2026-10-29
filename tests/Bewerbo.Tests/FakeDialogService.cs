using Bewerbo.Services;
using Bewerbo.ViewModels;

namespace Bewerbo.Tests;

internal sealed class FakeDialogService : IDialogService
{
    public EditorViewModel? LastEditor { get; private set; }

    public List<string> Errors { get; } = [];

    public List<string> Opened { get; } = [];

    /// <summary>Plays the user's part while a modal window would be open.</summary>
    public Action<SettingsViewModel>? OnSettings { get; set; }

    public Action<FolderImportViewModel>? OnFolderImport { get; set; }

    public IReadOnlyList<string> FilesToPick { get; set; } = [];

    public void ShowEditor(EditorViewModel editor) => LastEditor = editor;

    public void ShowSettings(SettingsViewModel settings) => OnSettings?.Invoke(settings);

    public void ShowFolderImport(FolderImportViewModel import) => OnFolderImport?.Invoke(import);

    public bool Confirm(string message, string title) => true;

    public void ShowInfo(string message, string title)
    {
    }

    public void ShowError(string message, string title) => Errors.Add(message);

    public string? PickExportFile(string suggestedName) => null;

    public string? PickImportFile() => null;

    public string? PickFolder(string title, string initialFolder) => null;

    public IReadOnlyList<string> PickFiles(string title) => FilesToPick;

    public void OpenLink(string link) => Opened.Add(link);

    public void OpenPath(string path) => Opened.Add(path);
}
