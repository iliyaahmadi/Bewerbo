using Bewerbo.Core;
using Bewerbo.Core.Documents;

namespace Bewerbo.ViewModels;

/// <summary>Creates applications from company folders that are not tracked yet.</summary>
public sealed class FolderImportViewModel : ObservableObject
{
    private readonly Func<IReadOnlyList<JobApplication>, bool> _create;
    private string _position;
    private string _type;
    private ApplicationStatus _status = ApplicationStatus.Applied;
    private string _error = "";

    public FolderImportViewModel(
        IReadOnlyList<FolderCandidate> candidates,
        string position,
        string type,
        IReadOnlyList<string> typeSuggestions,
        Func<IReadOnlyList<JobApplication>, bool> create)
    {
        _create = create;
        _position = position;
        _type = type;
        TypeSuggestions = typeSuggestions.Select(name => new Suggestion(name, new RelayCommand(() => Type = name))).ToList();
        Folders = candidates.Select(candidate => new FolderChoice(candidate, OnSelectionChanged)).ToList();
        SelectAllCommand = new RelayCommand(() => SelectAll(true));
        SelectNoneCommand = new RelayCommand(() => SelectAll(false));
        CreateCommand = new RelayCommand(Create);
    }

    public event EventHandler? CloseRequested;

    public IReadOnlyList<FolderChoice> Folders { get; }

    public IReadOnlyList<Suggestion> TypeSuggestions { get; }

    public IReadOnlyList<ApplicationStatus> Statuses { get; } = Enum.GetValues<ApplicationStatus>();

    public string Position { get => _position; set => SetProperty(ref _position, value); }

    public string Type { get => _type; set => SetProperty(ref _type, value); }

    public ApplicationStatus Status { get => _status; set => SetProperty(ref _status, value); }

    public string Error { get => _error; private set => SetProperty(ref _error, value); }

    public int SelectedCount => Folders.Count(f => f.IsSelected);

    public string CreateLabel => SelectedCount == 1 ? "Add 1 application" : $"Add {SelectedCount} applications";

    public RelayCommand SelectAllCommand { get; }

    public RelayCommand SelectNoneCommand { get; }

    public RelayCommand CreateCommand { get; }

    private void SelectAll(bool selected)
    {
        foreach (var folder in Folders)
            folder.IsSelected = selected;
    }

    private void OnSelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(CreateLabel));
    }

    private void Create()
    {
        var selected = Folders.Where(f => f.IsSelected).Select(f => f.Candidate).ToList();
        if (selected.Count == 0)
        {
            Error = "Select at least one folder.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Position))
        {
            Error = "Enter the position you applied for. You can change it per application later.";
            return;
        }

        if (_create(FolderImport.CreateApplications(selected, Position, Type, Status, DateTime.UtcNow)))
            CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}

public sealed class FolderChoice(FolderCandidate candidate, Action changed) : ObservableObject
{
    private bool _isSelected = true;

    public FolderCandidate Candidate { get; } = candidate;

    public string Company => Candidate.Company;

    public string Details
    {
        get
        {
            var files = Candidate.FileCount == 1 ? "1 file" : $"{Candidate.FileCount} files";
            return Candidate.FirstFileDate is { } date ? $"{files} · since {Dates.ToText(date)}" : files;
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
                changed();
        }
    }
}
