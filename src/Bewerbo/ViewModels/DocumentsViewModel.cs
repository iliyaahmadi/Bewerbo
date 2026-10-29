using System.Collections.ObjectModel;
using System.Windows.Input;
using Bewerbo.Core.Documents;

namespace Bewerbo.ViewModels;

/// <summary>The documents section of the editor for an application that already exists.</summary>
public sealed class DocumentsViewModel : ObservableObject
{
    private readonly string _folder;
    private readonly Action<string> _open;

    public DocumentsViewModel(string folder, Action<string> open, Action openFolder, Func<bool> addFiles)
    {
        _folder = folder;
        _open = open;
        OpenFolderCommand = new RelayCommand(openFolder);
        AddFilesCommand = new RelayCommand(() =>
        {
            if (addFiles())
                Reload();
        });
        Reload();
    }

    public string Folder => _folder;

    public ObservableCollection<DocumentItem> Files { get; } = [];

    public bool IsEmpty => Files.Count == 0;

    public ICommand OpenFolderCommand { get; }

    public ICommand AddFilesCommand { get; }

    public void Reload()
    {
        Files.Clear();
        foreach (var file in DocumentLibrary.List(_folder))
            Files.Add(new DocumentItem(file.Name, new RelayCommand(() => _open(file.Path))));
        OnPropertyChanged(nameof(IsEmpty));
    }
}

public sealed record DocumentItem(string Name, ICommand OpenCommand);
