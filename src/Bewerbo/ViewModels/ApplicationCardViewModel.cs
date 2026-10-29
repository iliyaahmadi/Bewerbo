using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Bewerbo.Core;
using Bewerbo.Core.Documents;
using Bewerbo.Services;

namespace Bewerbo.ViewModels;

public sealed class ApplicationCardViewModel : ObservableObject
{
    private readonly ICardActions _actions;
    private ApplicationStatus _status;
    private ImageSource? _icon;

    /// <param name="tagColor">Colour slot 0 to 3 for the type tag.</param>
    /// <param name="folder">The company folder's files, or null when no documents folder is set up.</param>
    public ApplicationCardViewModel(JobApplication application, DateOnly today, FaviconCache favicons, FolderState? folder, ICardActions actions, int tagColor = 0)
    {
        TagColor = tagColor;
        Folder = folder;
        Application = application;
        _actions = actions;
        _status = application.Status;
        IsFollowUpDue = application.IsFollowUpDue(today);
        Details = BuildDetails(application, IsFollowUpDue);
        EditCommand = new RelayCommand(() => actions.Edit(application));
        OpenLinkCommand = new RelayCommand(() => actions.OpenLink(application));
        FilesCommand = new RelayCommand(() => actions.ShowFiles(application));

        if (folder is not null)
        {
            FilesText = folder.Files.Count switch
            {
                0 => "No files",
                1 => "1 file",
                var count => $"{count} files",
            };
            FilesToolTip = folder.Files.Count > 0
                ? string.Join("\n", folder.Files.Select(f => f.Name)) + "\n\nClick to open, add or remove files."
                : "Click to add files, or drop them on this card.";
        }

        if (WebLink.Host(application.Link) is { } host)
            _ = LoadIconAsync(favicons, host);
    }

    public JobApplication Application { get; }

    public string Company => Application.Company;

    public string Position => Application.Position;

    public string Initials => string.Concat(Application.Company
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2)
        .Select(word => char.ToUpperInvariant(word[0])));

    public string TypeLabel => Application.Type;

    public bool HasType => Application.Type.Length > 0;

    public int TagColor { get; }

    public string Salary => Application.Salary;

    public bool HasSalary => Application.Salary.Length > 0;

    public string Details { get; }

    public bool HasDetails => Details.Length > 0;

    public bool IsFollowUpDue { get; }

    public string Notes => Application.Notes;

    public string NotesPreview => Application.Notes.ReplaceLineEndings(" · ");

    public bool HasNotes => Application.Notes.Length > 0;

    public bool HasLink => Application.Link.Length > 0;

    public FolderState? Folder { get; }

    public bool ShowFiles => Folder is not null;

    public string FilesText { get; } = "";

    public string FilesToolTip { get; } = "";

    public IReadOnlyList<ApplicationStatus> Statuses { get; } = Enum.GetValues<ApplicationStatus>();

    public ICommand EditCommand { get; }

    public ICommand OpenLinkCommand { get; }

    public ICommand FilesCommand { get; }

    public ImageSource? Icon
    {
        get => _icon;
        private set
        {
            if (SetProperty(ref _icon, value))
                OnPropertyChanged(nameof(HasIcon));
        }
    }

    public bool HasIcon => _icon is not null;

    public ApplicationStatus Status
    {
        get => _status;
        set
        {
            if (value == _status)
                return;

            _status = value;
            OnPropertyChanged();

            // Saving rebuilds the card list, so let the dropdown finish its own update first.
            Dispatcher.CurrentDispatcher.InvokeAsync(() =>
            {
                if (!_actions.ChangeStatus(Application, value))
                {
                    _status = Application.Status;
                    OnPropertyChanged(nameof(Status));
                }
            });
        }
    }

    public void AddFiles(IReadOnlyList<string> files) => _actions.AddFiles(Application, files);

    /// <summary>True when this card already shows exactly this state, so it can be kept as it is.</summary>
    public bool Shows(JobApplication application, int tagColor, FolderState? folder) =>
        ReferenceEquals(Application, application)
        && TagColor == tagColor
        && (Folder is null ? folder is null : folder is not null && Folder.Exists == folder.Exists && Folder.Files.SequenceEqual(folder.Files));

    private async Task LoadIconAsync(FaviconCache favicons, string host) =>
        Icon = await favicons.GetAsync(host);

    private static string BuildDetails(JobApplication application, bool followUpDue)
    {
        var parts = new List<string>();
        if (application.Location.Length > 0)
            parts.Add(application.Location);
        if (application.AppliedOn is { } applied)
            parts.Add("Applied " + Dates.ToText(applied));
        if (application.FollowUpOn is { } followUp)
            parts.Add((followUpDue ? "Follow-up due · " : "Follow up ") + Dates.ToText(followUp));
        return string.Join("  •  ", parts);
    }
}

/// <summary>A company folder as last read from disk.</summary>
public sealed record FolderState(bool Exists, IReadOnlyList<DocumentFile> Files);
