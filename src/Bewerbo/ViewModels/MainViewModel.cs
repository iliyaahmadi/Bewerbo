using System.Collections.ObjectModel;
using System.IO;
using Bewerbo.Core;
using Bewerbo.Core.Storage;
using Bewerbo.Services;

namespace Bewerbo.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly ApplicationStore _store;
    private readonly FaviconCache _favicons;
    private readonly IDialogService _dialogs;

    private List<JobApplication> _applications;
    private ApplicationType? _type;
    private ApplicationStatus? _status;
    private string? _positionKey;
    private string _search = "";
    private SortOption _sort;
    private string _title = "";
    private string _footer = "";
    private ApplicationSummary _summary;
    private bool _hasResults;

    public MainViewModel(ApplicationStore store, FaviconCache favicons, IDialogService dialogs)
    {
        _store = store;
        _favicons = favicons;
        _dialogs = dialogs;
        _applications = store.Load();
        _summary = ApplicationSummary.From(_applications, Today);
        _sort = SortOptions[0];

        AddCommand = new RelayCommand(Add);
        ExportCommand = new RelayCommand(Export);
        ImportCommand = new RelayCommand(Import);
        ClearSearchCommand = new RelayCommand(() => Search = "");
        ClearFiltersCommand = new RelayCommand(ClearFilters);

        Refresh();
    }

    public IReadOnlyList<SortOption> SortOptions { get; } =
    [
        new("Newest first", SortOrder.Newest),
        new("Recently updated", SortOrder.RecentlyUpdated),
        new("Company A–Z", SortOrder.Company),
        new("Follow-up date", SortOrder.FollowUpDate),
    ];

    public ObservableCollection<NavItem> Categories { get; } = [];

    public ObservableCollection<NavItem> Positions { get; } = [];

    public ObservableCollection<NavItem> StatusFilters { get; } = [];

    public ObservableCollection<ApplicationCardViewModel> Cards { get; } = [];

    public RelayCommand AddCommand { get; }

    public RelayCommand ExportCommand { get; }

    public RelayCommand ImportCommand { get; }

    public RelayCommand ClearSearchCommand { get; }

    public RelayCommand ClearFiltersCommand { get; }

    public string Search
    {
        get => _search;
        set
        {
            if (SetProperty(ref _search, value))
            {
                OnPropertyChanged(nameof(HasSearch));
                Refresh();
            }
        }
    }

    public bool HasSearch => _search.Length > 0;

    public SortOption Sort
    {
        get => _sort;
        set
        {
            if (SetProperty(ref _sort, value))
                Refresh();
        }
    }

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    public string Footer
    {
        get => _footer;
        private set => SetProperty(ref _footer, value);
    }

    public ApplicationSummary Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    public bool HasResults
    {
        get => _hasResults;
        private set
        {
            if (SetProperty(ref _hasResults, value))
                OnPropertyChanged(nameof(ShowEmptyState));
        }
    }

    public bool ShowEmptyState => !_hasResults;

    public bool HasNoApplications => _applications.Count == 0;

    public string EmptyTitle => HasNoApplications ? "No applications yet" : "No matching applications";

    public string EmptySubtitle => HasNoApplications
        ? "Add a job or Ausbildung to start tracking it."
        : "Try another filter or a different search.";

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    private void Refresh()
    {
        Summary = ApplicationSummary.From(_applications, Today);

        var inCategory = _applications.Where(a => _type is null || a.Type == _type).ToList();
        var groups = Core.Positions.Group(inCategory);
        if (_positionKey is not null && groups.All(g => g.Key != _positionKey))
            _positionKey = null;

        Replace(Categories,
        [
            Category("All applications", null),
            Category("Ausbildung", ApplicationType.Ausbildung),
            Category("Jobs", ApplicationType.Job),
        ]);

        Replace(Positions, groups.Select(group => new NavItem(
            group.Name,
            group.Count,
            group.Key == _positionKey,
            new RelayCommand(() => SelectPosition(group.Key)))));

        var inPosition = inCategory.Where(a => _positionKey is null || Core.Positions.Key(a.Position) == _positionKey).ToList();
        Replace(StatusFilters,
            new ApplicationStatus?[] { null }.Concat(Enum.GetValues<ApplicationStatus>().Cast<ApplicationStatus?>())
                .Select(status => new NavItem(
                    status?.ToString() ?? "All",
                    inPosition.Count(a => status is null || a.Status == status),
                    status == _status,
                    new RelayCommand(() => SelectStatus(status)))));

        var filter = new ApplicationFilter(_type, _status, _positionKey, _search);
        var today = Today;
        Replace(Cards, ApplicationQuery.Run(_applications, filter, _sort.Order)
            .Select(a => new ApplicationCardViewModel(a, today, _favicons, ChangeStatus, Edit, _dialogs.OpenLink)));

        Title = _positionKey is not null
            ? groups.First(g => g.Key == _positionKey).Name
            : _type switch
            {
                ApplicationType.Ausbildung => "Ausbildung",
                ApplicationType.Job => "Jobs",
                _ => "All applications",
            };

        HasResults = Cards.Count > 0;
        OnPropertyChanged(nameof(HasNoApplications));
        OnPropertyChanged(nameof(EmptyTitle));
        OnPropertyChanged(nameof(EmptySubtitle));
        Footer = $"{Cards.Count} application{(Cards.Count == 1 ? "" : "s")} shown · Changes save automatically";
    }

    private NavItem Category(string label, ApplicationType? type) => new(
        label,
        _applications.Count(a => type is null || a.Type == type),
        _type == type && _positionKey is null,
        new RelayCommand(() => SelectCategory(type)));

    private void SelectCategory(ApplicationType? type)
    {
        _type = type;
        _positionKey = null;
        Refresh();
    }

    private void SelectPosition(string key)
    {
        _positionKey = _positionKey == key ? null : key;
        Refresh();
    }

    private void SelectStatus(ApplicationStatus? status)
    {
        _status = status;
        Refresh();
    }

    private void ClearFilters()
    {
        _type = null;
        _status = null;
        _positionKey = null;
        _search = "";
        OnPropertyChanged(nameof(Search));
        OnPropertyChanged(nameof(HasSearch));
        Refresh();
    }

    private bool Commit(List<JobApplication> next)
    {
        try
        {
            _store.Save(next);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            _dialogs.ShowError("Your change could not be saved. Your previous data is unchanged.\n\n" + ex.Message, "Unable to save");
            return false;
        }

        _applications = next;
        Refresh();
        Footer = "Saved on this computer · " + DateTime.Now.ToString("HH:mm");
        return true;
    }

    private bool Upsert(JobApplication application) =>
        Commit([.. _applications.Where(a => a.Id != application.Id), application]);

    private bool ChangeStatus(JobApplication application, ApplicationStatus status) =>
        Upsert(application.WithStatus(status, DateTime.UtcNow, Today));

    private bool Delete(JobApplication application) =>
        _dialogs.Confirm($"Delete this application for {application.Company}?", "Delete application")
        && Commit(_applications.Where(a => a.Id != application.Id).ToList());

    private void Add()
    {
        var draft = ApplicationDraft.ForNew(_type ?? ApplicationType.Ausbildung, Today);
        _dialogs.ShowEditor(new EditorViewModel(draft, null, Upsert, Delete));
    }

    private void Edit(JobApplication application) =>
        _dialogs.ShowEditor(new EditorViewModel(ApplicationDraft.From(application), application, Upsert, Delete));

    private void Export()
    {
        var path = _dialogs.PickExportFile($"Bewerbo-backup-{DateTime.Now:yyyy-MM-dd}.json");
        if (path is null)
            return;

        try
        {
            File.WriteAllText(path, ApplicationStore.Serialize(_applications));
            Footer = "Backup exported";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _dialogs.ShowError(ex.Message, "Unable to export");
        }
    }

    private void Import()
    {
        var path = _dialogs.PickImportFile();
        if (path is null)
            return;

        List<JobApplication> imported;
        try
        {
            imported = ApplicationStore.Deserialize(File.ReadAllText(path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            _dialogs.ShowError("The backup could not be imported. Your applications are unchanged.\n\n" + ex.Message, "Unable to import");
            return;
        }

        var (merged, added) = ApplicationStore.Merge(_applications, imported);
        if (Commit(merged))
            _dialogs.ShowInfo($"{added} applications imported. Existing applications were kept.", "Backup imported");
    }

    private static void Replace<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
            collection.Add(item);
    }
}
