namespace Bewerbo.ViewModels;

/// <summary>The files window of one application.</summary>
public sealed class FilesViewModel(string company, string position, DocumentsViewModel documents)
{
    public string Company { get; } = company;

    public string Position { get; } = position;

    public DocumentsViewModel Documents { get; } = documents;
}
