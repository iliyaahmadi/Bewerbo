using Bewerbo.Services;
using Bewerbo.ViewModels;

namespace Bewerbo.Tests;

internal sealed class FakeDialogService : IDialogService
{
    public EditorViewModel? LastEditor { get; private set; }

    public List<string> Errors { get; } = [];

    public void ShowEditor(EditorViewModel editor) => LastEditor = editor;

    public bool Confirm(string message, string title) => true;

    public void ShowInfo(string message, string title)
    {
    }

    public void ShowError(string message, string title) => Errors.Add(message);

    public string? PickExportFile(string suggestedName) => null;

    public string? PickImportFile() => null;

    public void OpenLink(string link)
    {
    }
}
