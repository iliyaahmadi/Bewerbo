using System.Collections.ObjectModel;
using System.IO;
using Bewerbo.Core;
using Bewerbo.Core.Documents;
using Bewerbo.Core.Storage;
using Bewerbo.Services;

namespace Bewerbo.ViewModels;

public sealed class MainViewModel : ObservableObject, ICardActions
{
    private readonly ApplicationStore _store;
    private readonly SettingsStore _settingsStore;
    private readonly FaviconCache _favicons;
    private readonly IDialogService _dialogs;
    private readonly Action<AppTheme> _applyTheme;

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
    private Settings _settings;
    private DocumentLibrary? _library;

    public MainViewModel(ApplicationStore store, SettingsStore settingsStore, FaviconCache favicons, IDialogService dialogs, Action<AppTheme> applyTheme)
    {
        _store = store;
        _settingsStore = settingsStore;
        _favicons = favicons;
        _dialogs = dialogs;
        _applyTheme = applyTheme;
        _settings = settingsStore.Load();
        _library = CreateLibrary(_settings);
        _applications = store.Load();
        _summary = ApplicationSummary.From(_applications, Today);
        _sort = SortOptions[0];

        AddCommand = new RelayCommand(Add);
        ExportCommand = new RelayCommand(Export);
        ImportCommand = new RelayCommand(Import);
        ClearSearchCommand = new RelayCommand(() => Search = "");
        ClearFiltersCommand = new RelayCommand(ClearFilters);
        SettingsCommand = new RelayCommand(OpenSettings);
        ImportFoldersCommand = new RelayCommand(ImportFolders);

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

    public RelayCommand SettingsCommand { get; }

    public RelayCommand ImportFoldersCommand { get; }

    public Settings Settings => _settings.Clone();

    public bool HasDocumentsFolder => _library is not null;

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
        ? "Add a job or Ausbildung to start tracking it.\nCopied the details from a chat? Press Ctrl+V to fill the form in."
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
            .Select(a => new ApplicationCardViewModel(a, today, _favicons, _library, this)));

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

    public bool ChangeStatus(JobApplication application, ApplicationStatus status) =>
        Upsert(application.WithStatus(status, DateTime.UtcNow, Today));

    public void OpenLink(JobApplication application) => _dialogs.OpenLink(application.Link);

    public void OpenFolder(JobApplication application)
    {
        if (LinkFolder(application) is { } folder)
            _dialogs.OpenPath(folder);
    }

    public void AddFiles(JobApplication application, IReadOnlyList<string> files)
    {
        if (_library is null)
        {
            _dialogs.ShowInfo("Choose a documents folder in Settings first. Bewerbo then keeps one folder per company there.", "No documents folder");
            return;
        }

        if (LinkFolder(application) is not { } folder)
            return;

        try
        {
            var added = DocumentLibrary.AddFiles(folder, files);
            Refresh();
            Footer = added.Count == 1 ? $"1 file copied to {application.Company}" : $"{added.Count} files copied to {application.Company}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _dialogs.ShowError("The files could not be copied.\n\n" + ex.Message, "Unable to add files");
            Refresh();
        }
    }

    /// <summary>Opens the editor filled in from copied text, for Ctrl+V in the main window.</summary>
    /// <returns>False when the text holds no application details.</returns>
    public bool AddFromText(string? text)
    {
        if (DetailsParser.Parse(text).IsEmpty)
            return false;

        var editor = NewEditor();
        editor.Paste(text);
        _dialogs.ShowEditor(editor);
        return true;
    }

    private bool Delete(JobApplication application) =>
        _dialogs.Confirm($"Delete this application for {application.Company}?", "Delete application")
        && Commit(_applications.Where(a => a.Id != application.Id).ToList());

    private void Add() => _dialogs.ShowEditor(NewEditor());

    private EditorViewModel NewEditor()
    {
        var draft = ApplicationDraft.ForNew(_type ?? ApplicationType.Ausbildung, Today);
        var note = CreatesFolders ? $"A folder for the documents is created in {_library!.Root} when you save." : null;
        return new EditorViewModel(draft, null, SaveNew, Delete, newFolderNote: note);
    }

    public void Edit(JobApplication application)
    {
        DocumentsViewModel? documents = null;
        if (_library is not null)
        {
            documents = new DocumentsViewModel(
                _library.FolderFor(application),
                _dialogs.OpenPath,
                () => OpenFolder(Current(application)),
                () =>
                {
                    var files = _dialogs.PickFiles("Add documents to " + application.Company);
                    if (files.Count == 0)
                        return false;

                    AddFiles(Current(application), files);
                    return true;
                });
        }

        _dialogs.ShowEditor(new EditorViewModel(ApplicationDraft.From(application), application, SaveEdited, Delete, documents));
    }

    private bool CreatesFolders => _library is not null && _settings.CreateFolderForNewApplications;

    private bool SaveNew(JobApplication application)
    {
        if (CreatesFolders)
        {
            var folder = _library!.FolderFor(application);
            try
            {
                application.Folder = _library.Prepare(folder);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _dialogs.ShowError($"The application is saved, but its folder could not be created.\n\n{ex.Message}", "Unable to create folder");
            }
        }

        return Upsert(application);
    }

    private bool SaveEdited(JobApplication application)
    {
        // The folder can be linked while the editor is open, which the editor's copy does not know about.
        if (application.Folder.Length == 0)
            application.Folder = Current(application).Folder;
        return Upsert(application);
    }

    private JobApplication Current(JobApplication application) =>
        _applications.FirstOrDefault(a => a.Id == application.Id) ?? application;

    /// <summary>Creates the application's folder if needed and remembers it on the application.</summary>
    /// <returns>The folder, or null when there is no documents folder or it could not be created.</returns>
    private string? LinkFolder(JobApplication application)
    {
        if (_library is null)
            return null;

        var folder = _library.FolderFor(application);
        try
        {
            _library.Prepare(folder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _dialogs.ShowError(ex.Message, "Unable to create folder");
            return null;
        }

        if (application.Folder != folder)
        {
            var linked = application.Clone();
            linked.Folder = folder;
            Upsert(linked);
        }

        return folder;
    }

    private void OpenSettings()
    {
        var hadDocumentsFolder = _settings.DocumentsFolder;
        _dialogs.ShowSettings(new SettingsViewModel(_settings, _dialogs, SaveSettings));

        // Offer to pick up the existing company folders right after a documents folder is chosen.
        if (_library is not null && _settings.DocumentsFolder != hadDocumentsFolder
            && _library.FindUntracked(_applications).Count is > 0 and var count
            && _dialogs.Confirm($"{_settings.DocumentsFolder} has {count} company folder{(count == 1 ? "" : "s")} without an application. Add them now?", "Import folders"))
        {
            ImportFolders();
        }
    }

    private bool SaveSettings(Settings settings)
    {
        try
        {
            _settingsStore.Save(settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _dialogs.ShowError("Your settings could not be saved.\n\n" + ex.Message, "Unable to save");
            return false;
        }

        _settings = settings;
        _library = CreateLibrary(settings);
        _applyTheme(settings.Theme);
        OnPropertyChanged(nameof(HasDocumentsFolder));
        Refresh();
        return true;
    }

    private void ImportFolders()
    {
        if (_library is null)
            return;

        var candidates = _library.FindUntracked(_applications);
        if (candidates.Count == 0)
        {
            _dialogs.ShowInfo($"Every folder in {_library.Root} already belongs to an application.", "Import folders");
            return;
        }

        var commonPosition = _applications
            .Where(a => a.Type == ApplicationType.Ausbildung)
            .GroupBy(a => a.Position)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? "";

        _dialogs.ShowFolderImport(new FolderImportViewModel(candidates, commonPosition, created =>
        {
            if (!Commit([.. _applications, .. created]))
                return false;

            Footer = created.Count == 1 ? "1 application added from your folders" : $"{created.Count} applications added from your folders";
            return true;
        }));
    }

    private static DocumentLibrary? CreateLibrary(Settings settings) =>
        settings.HasDocumentsFolder ? new DocumentLibrary(settings.DocumentsFolder, settings.StandardDocumentsFolder) : null;

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
