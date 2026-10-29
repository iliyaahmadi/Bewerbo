using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Bewerbo.Core.Documents;

namespace Bewerbo.ViewModels;

/// <summary>
/// The documents section of the editor. Files the user adds are only remembered here and are
/// copied into the company folder when the application is saved, so Cancel leaves the folder alone.
/// </summary>
public sealed class DocumentsViewModel : ObservableObject
{
    private readonly string? _folder;
    private readonly Action<string> _open;
    private readonly List<string> _pending = [];

    /// <param name="folder">The application's existing folder, or null for a new application.</param>
    public DocumentsViewModel(string? folder, Func<IReadOnlyList<string>> pickFiles, Action<string> open, Action openFolder)
    {
        _folder = folder;
        _open = open;
        AddFilesCommand = new RelayCommand(() => Add(pickFiles()));
        OpenFolderCommand = new RelayCommand(openFolder);
        Reload();
    }

    public ObservableCollection<DocumentItem> Files { get; } = [];

    public IReadOnlyList<string> PendingFiles => _pending;

    public bool IsEmpty => Files.Count == 0;

    public bool HasFolder => _folder is not null && Directory.Exists(_folder);

    public string Folder => _folder ?? "";

    public ICommand AddFilesCommand { get; }

    public ICommand OpenFolderCommand { get; }

    public void Add(IEnumerable<string> files)
    {
        foreach (var file in files.Where(File.Exists))
        {
            if (!_pending.Contains(file, StringComparer.OrdinalIgnoreCase))
                _pending.Add(file);
        }

        Reload();
    }

    private void Reload()
    {
        Files.Clear();
        if (_folder is not null)
        {
            foreach (var file in DocumentLibrary.List(_folder))
                Files.Add(new DocumentItem(file.Name, IsNew: false, new RelayCommand(() => _open(file.Path)), null));
        }

        foreach (var path in _pending)
        {
            Files.Add(new DocumentItem(Path.GetFileName(path), IsNew: true, new RelayCommand(() => _open(path)), new RelayCommand(() =>
            {
                _pending.Remove(path);
                Reload();
            })));
        }

        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasFolder));
    }
}

/// <param name="RemoveCommand">Set for files that are not copied yet.</param>
public sealed record DocumentItem(string Name, bool IsNew, ICommand OpenCommand, ICommand? RemoveCommand);
