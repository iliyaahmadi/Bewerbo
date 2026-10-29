using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Bewerbo.Core.Documents;

namespace Bewerbo.ViewModels;

/// <summary>
/// The files of one application. For an application that exists, adding and removing act on its
/// folder straight away. For a new application the files are collected and copied when it is saved.
/// </summary>
public sealed class DocumentsViewModel : ObservableObject
{
    private readonly string? _folder;
    private readonly IFileActions _actions;
    private readonly List<string> _pending = [];
    private readonly HashSet<string> _standard = new(StringComparer.OrdinalIgnoreCase);

    private DocumentsViewModel(string? folder, IFileActions actions)
    {
        _folder = folder;
        _actions = actions;
        AddFilesCommand = new RelayCommand(() => Add(actions.PickFiles()));
        OpenFolderCommand = new RelayCommand(() =>
        {
            actions.OpenFolder();
            Reload();
        });
    }

    /// <summary>The files in an existing application's folder.</summary>
    public static DocumentsViewModel ForFolder(string folder, IFileActions actions)
    {
        var documents = new DocumentsViewModel(folder, actions);
        documents.Reload();
        return documents;
    }

    /// <summary>The files a new application will start with: the standard documents plus whatever the user adds.</summary>
    public static DocumentsViewModel ForNew(IEnumerable<string> standardDocuments, IFileActions actions)
    {
        var documents = new DocumentsViewModel(null, actions);
        foreach (var file in standardDocuments)
        {
            documents._standard.Add(file);
            documents._pending.Add(file);
        }

        documents.Reload();
        return documents;
    }

    public ObservableCollection<DocumentItem> Files { get; } = [];

    /// <summary>Files to copy into the folder when a new application is saved.</summary>
    public IReadOnlyList<string> PendingFiles => _pending;

    public bool IsNew => _folder is null;

    public bool IsEmpty => Files.Count == 0;

    public bool HasFolder => _folder is not null && Directory.Exists(_folder);

    public string Folder => _folder ?? "";

    public string Summary => Files.Count == 1 ? "1 file" : $"{Files.Count} files";

    public ICommand AddFilesCommand { get; }

    public ICommand OpenFolderCommand { get; }

    public void Add(IReadOnlyList<string> files)
    {
        if (files.Count == 0)
            return;

        if (_folder is null)
        {
            foreach (var file in files.Where(File.Exists))
            {
                if (!_pending.Contains(file, StringComparer.OrdinalIgnoreCase))
                    _pending.Add(file);
            }
        }
        else
        {
            _actions.AddFiles(files);
        }

        Reload();
    }

    public void Reload()
    {
        Files.Clear();
        if (_folder is not null)
        {
            foreach (var file in DocumentLibrary.List(_folder))
            {
                Files.Add(new DocumentItem(
                    file.Name,
                    $"{FormatSize(file.Size)} · {file.Modified:yyyy-MM-dd HH:mm}",
                    new RelayCommand(() => _actions.Open(file.Path)),
                    new RelayCommand(() =>
                    {
                        if (_actions.Remove(file.Path))
                            Reload();
                    }),
                    "Move to the Recycle Bin"));
            }
        }

        foreach (var path in _pending)
        {
            Files.Add(new DocumentItem(
                Path.GetFileName(path),
                _standard.Contains(path) ? "Standard document · copied when you save" : "Copied when you save",
                new RelayCommand(() => _actions.Open(path)),
                new RelayCommand(() =>
                {
                    _pending.Remove(path);
                    Reload();
                }),
                "Don't add this file"));
        }

        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasFolder));
        OnPropertyChanged(nameof(Summary));
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0} KB",
        _ => $"{bytes / (1024.0 * 1024):0.0} MB",
    };
}

public sealed record DocumentItem(string Name, string Detail, ICommand OpenCommand, ICommand RemoveCommand, string RemoveToolTip);

/// <summary>What the files list asks the main view model to do with real files.</summary>
public interface IFileActions
{
    IReadOnlyList<string> PickFiles();

    void Open(string path);

    void OpenFolder();

    void AddFiles(IReadOnlyList<string> files);

    /// <returns>True when the file was moved to the Recycle Bin.</returns>
    bool Remove(string path);
}
