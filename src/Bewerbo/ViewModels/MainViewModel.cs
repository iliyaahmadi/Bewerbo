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
    private AdvancedFilter _advanced = AdvancedFilter.None;
    private string _search = "";
    private SortOption _sort;
    private string _title = "";
    private string _footer = "";
    private ApplicationSummary _summary;
    private bool _hasResults;
    private Settings _settings;
    private DocumentLibrary _library;

    // Folder listings for the cards, so filtering and sorting never touch the disk.
    private readonly Dictionary<string, FolderState> _folders = new(StringComparer.OrdinalIgnoreCase);

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
        OpenFiltersCommand = new RelayCommand(OpenFilters);
        ClearAdvancedCommand = new RelayCommand(() => ApplyAdvanced(AdvancedFilter.None));

        Refresh();
    }

    public IReadOnlyList<SortOption> SortOptions { get; } =
    [
        new("Newest first", SortOrder.Newest),
        new("Recently updated", SortOrder.RecentlyUpdated),
        new("Company A–Z", SortOrder.Company),
        new("Follow-up date", SortOrder.FollowUpDate),
        new("Advanced filters…", null),
    ];

    public ObservableCollection<NavItem> Categories { get; } = [];

    public ObservableCollection<NavItem> StatusFilters { get; } = [];

    public ObservableCollection<ApplicationCardViewModel> Cards { get; } = [];

    public RelayCommand AddCommand { get; }

    public RelayCommand ExportCommand { get; }

    public RelayCommand ImportCommand { get; }

    public RelayCommand ClearSearchCommand { get; }

    public RelayCommand ClearFiltersCommand { get; }

    public RelayCommand SettingsCommand { get; }

    public RelayCommand ImportFoldersCommand { get; }

    public RelayCommand OpenFiltersCommand { get; }

    public RelayCommand ClearAdvancedCommand { get; }

    /// <summary>The criteria set in the Advanced filters window.</summary>
    public AdvancedFilter Advanced => _advanced;

    public bool HasAdvancedFilters => !_advanced.IsEmpty;

    public string AdvancedFiltersLabel => _advanced.Count == 1 ? "1 filter" : $"{_advanced.Count} filters";

    public Settings Settings => _settings.Clone();

    public string DocumentsFolder => _library.Root;

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
            if (value.Order is null)
            {
                // "Advanced filters…" is not a sort order: keep the current one and open the window
                // once the dropdown has closed.
                OnPropertyChanged(nameof(Sort));
                System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeAsync(() =>
                {
                    OnPropertyChanged(nameof(Sort));
                    OpenFilters();
                });
                return;
            }

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

    private void Refresh()
    {
        Summary = ApplicationSummary.From(_applications, Today);

        var types = ApplicationTypes.Group(_applications);
        if (_typeKey is not null && types.All(t => t.Key != _typeKey))
            _typeKey = null;

        var filtered = _applications
            .Where(a => (_typeKey is null || Labels.Key(a.Type) == _typeKey) && _advanced.Matches(a, Today))
            .ToList();

        Replace(Categories,
            types.Select(type => new NavItem(type.Name, type.Count, type.Key == _typeKey, new RelayCommand(() => SelectType(type.Key))))
                .Prepend(new NavItem("All applications", _applications.Count, _typeKey is null, new RelayCommand(() => SelectType(null)))));

        Replace(StatusFilters,
            new ApplicationStatus?[] { null }.Concat(Enum.GetValues<ApplicationStatus>().Cast<ApplicationStatus?>())
                .Select(status => new NavItem(
                    status?.ToString() ?? "All",
                    filtered.Count(a => status is null || a.Status == status),
                    status == _status,
                    new RelayCommand(() => SelectStatus(status)))));

        SyncCards(types);

        // A single position picked in Advanced filters names the list; otherwise the category does.
        Title = (_advanced.PositionKeys.Count == 1
                ? _applications.FirstOrDefault(a => _advanced.PositionKeys.Contains(Core.Positions.Key(a.Position)))?.Position.Trim()
                : null)
            ?? types.FirstOrDefault(t => t.Key == _typeKey)?.Name
            ?? "All applications";

        OnPropertyChanged(nameof(Advanced));
        OnPropertyChanged(nameof(HasAdvancedFilters));
        OnPropertyChanged(nameof(AdvancedFiltersLabel));

        HasResults = Cards.Count > 0;
        OnPropertyChanged(nameof(HasNoApplications));
        OnPropertyChanged(nameof(EmptyTitle));
        OnPropertyChanged(nameof(EmptySubtitle));
        Footer = $"{Cards.Count} application{(Cards.Count == 1 ? "" : "s")} shown · Changes save automatically";
    }

    private void SelectType(string? key)
    {
        _typeKey = key;
        Refresh();
    }

    /// <summary>Opens the Advanced filters window for the positions in the current category.</summary>
    private void OpenFilters()
    {
        var inType = _applications.Where(a => _typeKey is null || Labels.Key(a.Type) == _typeKey);
        _dialogs.ShowFilters(new FiltersViewModel(Core.Positions.Group(inType), _advanced, ApplyAdvanced));
    }

    private void ApplyAdvanced(AdvancedFilter filter)
    {
        _advanced = filter;
        Refresh();
    }

    /// <summary>
    /// Updates the card list in place. Cards whose application, tag colour and files are unchanged are kept,
    /// so the list does not jump back to the top or redraw every card after each change.
    /// </summary>
    private void SyncCards(List<LabelGroup> types)
    {
        var filter = new ApplicationFilter(_typeKey, _status, _advanced, _search, Today);
        var allTypes = types.Select(t => t.Name).ToList();
        var applications = ApplicationQuery.Run(_applications, filter, _sort.Order ?? SortOrder.Newest);

        for (var i = 0; i < applications.Count; i++)
        {
            var application = applications[i];
            var color = ApplicationTypes.ColorIndex(application.Type, allTypes);
            var folder = FolderOf(application);
            if (i < Cards.Count && Cards[i].Shows(application, color, folder))
                continue;

            var card = new ApplicationCardViewModel(application, Today, _favicons, folder, this, color);
            if (i < Cards.Count)
                Cards[i] = card;
            else
                Cards.Add(card);
        }

        while (Cards.Count > applications.Count)
            Cards.RemoveAt(Cards.Count - 1);
    }

    private FolderState FolderOf(JobApplication application)
    {
        var folder = _library.FolderFor(application);
        if (!_folders.TryGetValue(folder, out var state))
            _folders[folder] = state = new FolderState(Directory.Exists(folder), DocumentLibrary.List(folder));
        return state;
    }

    /// <summary>Reads the company folders again, for when files may have changed outside the app.</summary>
    public void RefreshFiles()
    {
        _folders.Clear();
        SyncCards(ApplicationTypes.Group(_applications));
    }

    private void FilesChanged(string folder)
    {
        _folders.Remove(folder);
        SyncCards(ApplicationTypes.Group(_applications));
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
        _advanced = AdvancedFilter.None;
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

    public void ShowFiles(JobApplication application)
    {
        var documents = DocumentsViewModel.ForFolder(_library.FolderFor(application), new ApplicationFiles(this, application));
        _dialogs.ShowFiles(new FilesViewModel(application.Company, application.Position, documents));
    }

    public void AddFiles(JobApplication application, IReadOnlyList<string> files)
    {
        if (LinkFolder(application) is not { } folder)
            return;

        try
        {
            var added = DocumentLibrary.AddFiles(folder, files);
            Footer = added.Count == 1 ? $"1 file copied to {application.Company}" : $"{added.Count} files copied to {application.Company}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _dialogs.ShowError("The files could not be copied.\n\n" + ex.Message, "Unable to add files");
        }

        FilesChanged(folder);
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
        var documents = DocumentsViewModel.ForNew(_library.StandardDocuments.Select(d => d.Path), new NewApplicationFiles(_dialogs));
        var note = $"Files are copied to a folder for this company in {_library.Root} when you save.";

        return new EditorViewModel(
            ApplicationDraft.ForNew(type, Today),
            null,
            application => SaveNew(application, documents.PendingFiles),
            Delete,
            TypeSuggestions(types),
            documents,
            note);
    }

    public void Edit(JobApplication application)
    {
        var documents = DocumentsViewModel.ForFolder(_library.FolderFor(application), new ApplicationFiles(this, application));
        _dialogs.ShowEditor(new EditorViewModel(
            ApplicationDraft.From(application),
            application,
            SaveEdited,
            Delete,
            TypeSuggestions(ApplicationTypes.Group(_applications)),
            documents));
    }

    private static IReadOnlyList<string> TypeSuggestions(List<LabelGroup> types) =>
        types.Count > 0 ? types.Select(t => t.Name).ToList() : ApplicationTypes.Suggestions;

    /// <summary>Saves a new application and copies the files collected in the form into its folder.</summary>
    private bool SaveNew(JobApplication application, IReadOnlyList<string> files)
    {
        if (files.Count > 0)
        {
            var folder = _library.FolderFor(application);
            try
            {
                DocumentLibrary.AddFiles(folder, files);
                application.Folder = folder;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _dialogs.ShowError($"The application is saved, but its files could not be copied.\n\n{ex.Message}", "Unable to copy files");
            }

            _folders.Remove(folder);
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

    private bool RemoveFile(string path)
    {
        if (!_dialogs.Confirm($"Move {Path.GetFileName(path)} to the Recycle Bin?", "Remove file") || !_dialogs.MoveToRecycleBin(path))
            return false;

        FilesChanged(Path.GetDirectoryName(path)!);
        return true;
    }

    /// <summary>File actions for an application that already exists: they act on its folder straight away.</summary>
    private sealed class ApplicationFiles(MainViewModel main, JobApplication application) : IFileActions
    {
        public IReadOnlyList<string> PickFiles() => main._dialogs.PickFiles("Add files for " + application.Company);

        public void Open(string path) => main._dialogs.OpenPath(path);

        public void OpenFolder() => main.OpenFolder(main.Current(application));

        public void AddFiles(IReadOnlyList<string> files) => main.AddFiles(main.Current(application), files);

        public bool Remove(string path) => main.RemoveFile(path);
    }

    /// <summary>File actions for a new application, whose files are only collected until it is saved.</summary>
    private sealed class NewApplicationFiles(IDialogService dialogs) : IFileActions
    {
        public IReadOnlyList<string> PickFiles() => dialogs.PickFiles("Add files");

        public void Open(string path) => dialogs.OpenPath(path);

        public void OpenFolder()
        {
        }

        public void AddFiles(IReadOnlyList<string> files)
        {
        }

        public bool Remove(string path) => false;
    }

    private JobApplication Current(JobApplication application) =>
        _applications.FirstOrDefault(a => a.Id == application.Id) ?? application;

    /// <summary>Creates the application's folder if needed and remembers it on the application.</summary>
    /// <returns>The folder, or null when it could not be created.</returns>
    private string? LinkFolder(JobApplication application)
    {
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

        _folders.Remove(folder);
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
        var hadDocumentsFolder = _library.Root;
        _dialogs.ShowSettings(new SettingsViewModel(_settings, _dialogs, SaveSettings));

        // Offer to pick up the existing company folders right after a documents folder is chosen.
        if (_library.Root != hadDocumentsFolder
            && _library.FindUntracked(_applications).Count is > 0 and var count
            && _dialogs.Confirm($"{_library.Root} has {count} company folder{(count == 1 ? "" : "s")} without an application. Add them now?", "Import folders"))
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
        _folders.Clear();
        _applyTheme(settings.Theme);
        OnPropertyChanged(nameof(DocumentsFolder));
        Refresh();
        return true;
    }

    private void ImportFolders()
    {
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

    private static DocumentLibrary CreateLibrary(Settings settings) =>
        new(settings.DocumentsFolderOrDefault, settings.StandardDocumentsFolder);

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
