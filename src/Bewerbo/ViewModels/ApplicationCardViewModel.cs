using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Bewerbo.Core;
using Bewerbo.Services;

namespace Bewerbo.ViewModels;

public sealed class ApplicationCardViewModel : ObservableObject
{
    private readonly Func<JobApplication, ApplicationStatus, bool> _changeStatus;
    private ApplicationStatus _status;
    private ImageSource? _icon;

    public ApplicationCardViewModel(
        JobApplication application,
        DateOnly today,
        FaviconCache favicons,
        Func<JobApplication, ApplicationStatus, bool> changeStatus,
        Action<JobApplication> edit,
        Action<string> openLink)
    {
        Application = application;
        _changeStatus = changeStatus;
        _status = application.Status;
        IsFollowUpDue = application.IsFollowUpDue(today);
        Details = BuildDetails(application, IsFollowUpDue);
        EditCommand = new RelayCommand(() => edit(application));
        OpenLinkCommand = new RelayCommand(() => openLink(application.Link));

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

    public IReadOnlyList<ApplicationStatus> Statuses { get; } = Enum.GetValues<ApplicationStatus>();

    public ICommand EditCommand { get; }

    public ICommand OpenLinkCommand { get; }

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
                if (!_changeStatus(Application, value))
                {
                    _status = Application.Status;
                    OnPropertyChanged(nameof(Status));
                }
            });
        }
    }

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
