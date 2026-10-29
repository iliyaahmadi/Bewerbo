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
    private string? _typeKey;
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

    public ObservableCollection<NavItem> PositionFilters { get; } = [];

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
        ? "Add an application to start tracking it.\nCopied the details from a chat? Press Ctrl+V to fill the form in."
        : "Try another filter or a different search.";

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    public bool HasPositionFilters => PositionFilters.Count > 2;

    private void Refresh()
    {
        Summary = ApplicationSummary.From(_applications, Today);

        var types = ApplicationTypes.Group(_applications);
        if (_typeKey is not null && types.All(t => t.Key != _typeKey))
            _typeKey = null;

        var inType = _applications.Where(a => _typeKey is null || Labels.Key(a.Type) == _typeKey).ToList();
        var positions = Core.Positions.Group(inType);
        if (_positionKey is not null && positions.All(g => g.Key != _positionKey))
            _positionKey = null;

        Replace(Categories,
            types.Select(type => new NavItem(type.Name, type.Count, type.Key == _typeKey, new RelayCommand(() => SelectType(type.Key))))
                .Prepend(new NavItem("All applications", _applications.Count, _typeKey is null, new RelayCommand(() => SelectType(null)))));

        Replace(PositionFilters,
            positions.Select(group => new NavItem(group.Name, group.Count, group.Key == _positionKey, new RelayCommand(() => SelectPosition(group.Key))))
                .Prepend(new NavItem("All", inType.Count, _positionKey is null, new RelayCommand(() => SelectPosition(null)))));
        OnPropertyChanged(nameof(HasPositionFilters));

        var inPosition = inType.Where(a => _positionKey is null || Core.Positions.Key(a.Position) == _positionKey).ToList();
        Replace(StatusFilters,
            new ApplicationStatus?[] { null }.Concat(Enum.GetValues<ApplicationStatus>().Cast<ApplicationStatus?>())
                .Select(status => new NavItem(
                    status?.ToString() ?? "All",
                    inPosition.Count(a => status is null || a.Status == status),
                    status == _status,
                    new RelayCommand(() => SelectStatus(status)))));

        var filter = new ApplicationFilter(_typeKey, _status, _positionKey, _search);
        var today = Today;
        var allTypes = types.Select(t => t.Name).ToList();
        Replace(Cards, ApplicationQuery.Run(_applications, filter, _sort.Order)
            .Select(a => new ApplicationCardViewModel(a, today, _favicons, _library, this, ApplicationTypes.ColorIndex(a.Type, allTypes))));

        Title = positions.FirstOrDefault(g => g.Key == _positionKey)?.Name
            ?? types.FirstOrDefault(t => t.Key == _typeKey)?.Name
            ?? "All applications";

        HasResults = Cards.Count > 0;
        OnPropertyChanged(nameof(HasNoApplications));
        OnPropertyChanged(nameof(EmptyTitle));
        OnPropertyChanged(nameof(EmptySubtitle));
        Footer = $"{Cards.Count} application{(Cards.Count == 1 ? "" : "s")} shown · Changes save automatically";
    }

    private void SelectType(string? key)
    {
        _typeKey = key;
        _positionKey = null;
        Refresh();
    }

    private void SelectPosition(string? key)
    {
        _positionKey = key;
        Refresh();
    }

    private void SelectStatus(ApplicationStatus? status)
    {
        _status = status;
        Refresh();
    }

    private void ClearFilters()
    {
        _typeKey = null;
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

        if (LinkFolder(application, files) is not { } folder)
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
        var types = ApplicationTypes.Group(_applications);
        var type = types.FirstOrDefault(t => t.Key == _typeKey)?.Name ?? types.FirstOrDefault()?.Name ?? ApplicationTypes.Suggestions[0];
        var documents = _library is null ? null : NewDocuments(null, null);
        var note = _library is not null && _settings.CreateFolderForNewApplications
            ? $"A folder for this company is created in {_library.Root} when you save."
            : null;

        return new EditorViewModel(
            ApplicationDraft.ForNew(type, Today),
            null,
            application => SaveWithDocuments(application, documents?.PendingFiles ?? [], isNew: true),
            Delete,
            TypeSuggestions(types),
            documents,
            note);
    }

    public void Edit(JobApplication application)
    {
        var documents = _library is null ? null : NewDocuments(_library.FolderFor(application), application);
        _dialogs.ShowEditor(new EditorViewModel(
            ApplicationDraft.From(application),
            application,
            edited => SaveWithDocuments(edited, documents?.PendingFiles ?? [], isNew: false),
            Delete,
            TypeSuggestions(ApplicationTypes.Group(_applications)),
            documents));
    }

    private static IReadOnlyList<string> TypeSuggestions(List<LabelGroup> types) =>
        types.Count > 0 ? types.Select(t => t.Name).ToList() : ApplicationTypes.Suggestions;

    private DocumentsViewModel NewDocuments(string? folder, JobApplication? application) => new(
        folder,
        () => _dialogs.PickFiles("Add documents"),
        _dialogs.OpenPath,
        () =>
        {
            if (application is not null)
                OpenFolder(Current(application));
        });

    /// <summary>Saves the application and copies the files added in the editor into its folder.</summary>
    private bool SaveWithDocuments(JobApplication application, IReadOnlyList<string> files, bool isNew)
    {
        // The folder can be linked while the editor is open, which the editor's copy does not know about.
        if (!isNew && application.Folder.Length == 0)
            application.Folder = Current(application).Folder;

        if (_library is not null && (files.Count > 0 || (isNew && _settings.CreateFolderForNewApplications)))
        {
            try
            {
                var folder = _library.Prepare(_library.FolderFor(application), files);
                application.Folder = folder;
                DocumentLibrary.AddFiles(folder, files);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _dialogs.ShowError($"The application is saved, but its documents could not be copied.\n\n{ex.Message}", "Unable to copy documents");
            }
        }

        return Upsert(application);
    }

    private JobApplication Current(JobApplication application) =>
        _applications.FirstOrDefault(a => a.Id == application.Id) ?? application;

    /// <summary>Creates the application's folder if needed and remembers it on the application.</summary>
    /// <returns>The folder, or null when there is no documents folder or it could not be created.</returns>
    private string? LinkFolder(JobApplication application, IReadOnlyCollection<string>? ownFiles = null)
    {
        if (_library is null)
            return null;

        var folder = _library.FolderFor(application);
        try
        {
            _library.Prepare(folder, ownFiles);
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

        var types = ApplicationTypes.Group(_applications);
        var type = types.FirstOrDefault(t => t.Key == _typeKey)?.Name ?? types.FirstOrDefault()?.Name ?? "";
        var position = Core.Positions.Group(_applications.Where(a => Labels.Key(a.Type) == Labels.Key(type))).FirstOrDefault()?.Name ?? "";

        _dialogs.ShowFolderImport(new FolderImportViewModel(candidates, position, type, TypeSuggestions(types), created =>
        {
            if (!Commit([.. _applications, .. created]))
                return false;

            Footer = created.Count == 1 ? "1 application added from your folders" : $"{created.Count} applications added from your folders";
            return true;
        }));
    }

    private static DocumentLibrary? CreateLibrary(Settings settings) =>
        settings.HasDocumentsFolder ? new DocumentLibrary(settings.DocumentsFolder, settings.StandardDocumentsFolder, settings.Checklist) : null;

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
