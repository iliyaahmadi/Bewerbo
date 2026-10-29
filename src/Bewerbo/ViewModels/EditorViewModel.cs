using Bewerbo.Core;

namespace Bewerbo.ViewModels;

public sealed class EditorViewModel : ObservableObject
{
    private readonly ApplicationDraft _draft;
    private readonly JobApplication? _original;
    private readonly Func<JobApplication, bool> _save;
    private readonly Func<JobApplication, bool> _delete;
    private string _error = "";

    public EditorViewModel(ApplicationDraft draft, JobApplication? original, Func<JobApplication, bool> save, Func<JobApplication, bool> delete)
    {
        _draft = draft;
        _original = original;
        _save = save;
        _delete = delete;
        SaveCommand = new RelayCommand(Save);
        DeleteCommand = new RelayCommand(Delete);
    }

    public event EventHandler? CloseRequested;

    public string Title => _original is null ? "Add application" : "Edit application";

    public bool CanDelete => _original is not null;

    public IReadOnlyList<ApplicationType> Types { get; } = Enum.GetValues<ApplicationType>();

    public IReadOnlyList<ApplicationStatus> Statuses { get; } = Enum.GetValues<ApplicationStatus>();

    public RelayCommand SaveCommand { get; }

    public RelayCommand DeleteCommand { get; }

    public string Error
    {
        get => _error;
        private set => SetProperty(ref _error, value);
    }

    public string Company { get => _draft.Company; set => Set(value, v => _draft.Company = v, _draft.Company); }

    public string Position { get => _draft.Position; set => Set(value, v => _draft.Position = v, _draft.Position); }

    public ApplicationType Type { get => _draft.Type; set => Set(value, v => _draft.Type = v, _draft.Type); }

    public ApplicationStatus Status { get => _draft.Status; set => Set(value, v => _draft.Status = v, _draft.Status); }

    public string Location { get => _draft.Location; set => Set(value, v => _draft.Location = v, _draft.Location); }

    public string AppliedOn { get => _draft.AppliedOn; set => Set(value, v => _draft.AppliedOn = v, _draft.AppliedOn); }

    public string FollowUpOn { get => _draft.FollowUpOn; set => Set(value, v => _draft.FollowUpOn = v, _draft.FollowUpOn); }

    public string Salary { get => _draft.Salary; set => Set(value, v => _draft.Salary = v, _draft.Salary); }

    public string Link { get => _draft.Link; set => Set(value, v => _draft.Link = v, _draft.Link); }

    public string Notes { get => _draft.Notes; set => Set(value, v => _draft.Notes = v, _draft.Notes); }

    /// <returns>False when the text had no fields to fill in.</returns>
    public bool Paste(string? text)
    {
        var details = DetailsParser.Parse(text);
        if (details.IsEmpty)
        {
            Error = "Nothing to paste. Copy lines like \"Company: …\" and \"Position: …\" first.";
            return false;
        }

        _draft.Fill(details);
        Error = "";
        OnPropertyChanged(string.Empty);
        return true;
    }

    private void Save()
    {
        if (_draft.Validate() is { } error)
        {
            Error = error;
            return;
        }

        var application = _draft.Build(_original, DateTime.UtcNow, DateOnly.FromDateTime(DateTime.Today));
        if (_save(application))
            CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Delete()
    {
        if (_original is not null && _delete(_original))
            CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Set<T>(T value, Action<T> assign, T current, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(value, current))
            return;

        assign(value);
        OnPropertyChanged(name);
    }
}
