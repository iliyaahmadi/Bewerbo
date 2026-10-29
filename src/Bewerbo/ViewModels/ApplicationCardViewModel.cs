using System.IO;
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

    public ApplicationCardViewModel(JobApplication application, DateOnly today, FaviconCache favicons, DocumentLibrary? library, ICardActions actions)
    {
        Application = application;
        _actions = actions;
        _status = application.Status;
        IsFollowUpDue = application.IsFollowUpDue(today);
        Details = BuildDetails(application, IsFollowUpDue);
        EditCommand = new RelayCommand(() => actions.Edit(application));
        OpenLinkCommand = new RelayCommand(() => actions.OpenLink(application));
        OpenFolderCommand = new RelayCommand(() => actions.OpenFolder(application));

        if (library is not null)
        {
            ShowDocuments = true;
            var folder = library.FolderFor(application);
            HasFolder = Directory.Exists(folder);
            var files = DocumentLibrary.List(folder);
            var kinds = files.Aggregate(DocumentKinds.None, (all, file) => all | file.Kinds);
            Documents =
            [
                new DocumentStatus("Anschreiben", kinds.HasFlag(DocumentKinds.CoverLetter)),
                new DocumentStatus("Lebenslauf", kinds.HasFlag(DocumentKinds.Resume)),
                new DocumentStatus("Zeugnisse", kinds.HasFlag(DocumentKinds.Certificates)),
            ];
            FileCountText = !HasFolder ? "No folder yet" : files.Count == 1 ? "1 file" : $"{files.Count} files";
            FilesToolTip = files.Count > 0
                ? string.Join("\n", files.Select(f => f.Name))
                : "Drop files on this card to copy them into the company folder.";
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

    public bool IsAusbildung => Application.Type == ApplicationType.Ausbildung;

    public string TypeLabel => Application.Type.ToString();

    public string Salary => Application.Salary;

    public bool HasSalary => Application.Salary.Length > 0;

    public string Details { get; }

    public bool HasDetails => Details.Length > 0;

    public bool IsFollowUpDue { get; }

    public string Notes => Application.Notes;

    public string NotesPreview => Application.Notes.ReplaceLineEndings(" · ");

    public bool HasNotes => Application.Notes.Length > 0;

    public bool HasLink => Application.Link.Length > 0;

    public bool ShowDocuments { get; }

    public bool HasFolder { get; }

    /// <summary>Whether the cover letter, CV and certificates are in the company folder.</summary>
    public IReadOnlyList<DocumentStatus> Documents { get; } = [];

    public string FileCountText { get; } = "";

    public string FilesToolTip { get; } = "";

    public IReadOnlyList<ApplicationStatus> Statuses { get; } = Enum.GetValues<ApplicationStatus>();

    public ICommand EditCommand { get; }

    public ICommand OpenLinkCommand { get; }

    public ICommand OpenFolderCommand { get; }

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

public sealed record DocumentStatus(string Label, bool IsPresent)
{
    public string Text => IsPresent ? "✓ " + Label : Label;

    public string ToolTip => IsPresent ? Label + " is in the folder" : "No " + Label + " in the folder yet";
}
