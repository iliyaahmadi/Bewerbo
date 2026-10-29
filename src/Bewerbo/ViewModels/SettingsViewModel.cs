using Bewerbo.Core;
using Bewerbo.Core.Documents;
using Bewerbo.Services;

namespace Bewerbo.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly Settings _settings;
    private readonly IDialogService _dialogs;
    private readonly Func<Settings, bool> _save;
    private string _checklistText;

    public SettingsViewModel(Settings settings, IDialogService dialogs, Func<Settings, bool> save)
    {
        _settings = settings.Clone();
        _checklistText = DocumentChecklist.ToText(settings.Checklist);
        ResetChecklistCommand = new RelayCommand(() => ChecklistText = DocumentChecklist.ToText(DocumentChecklist.Default));
        _dialogs = dialogs;
        _save = save;
        BrowseDocumentsCommand = new RelayCommand(() =>
            DocumentsFolder = Pick("Choose the folder for your application documents", DocumentsFolder) ?? DocumentsFolder);
        BrowseStandardDocumentsCommand = new RelayCommand(() =>
            StandardDocumentsFolder = Pick("Choose the folder with the documents every application gets", StandardDocumentsFolder) ?? StandardDocumentsFolder);
        ClearDocumentsCommand = new RelayCommand(() => DocumentsFolder = "");
        ClearStandardDocumentsCommand = new RelayCommand(() => StandardDocumentsFolder = "");
        SaveCommand = new RelayCommand(Save);
    }

    public event EventHandler? CloseRequested;

    public bool UseSystemTheme { get => _settings.Theme == AppTheme.System; set => SetTheme(value, AppTheme.System); }

    public bool UseLightTheme { get => _settings.Theme == AppTheme.Light; set => SetTheme(value, AppTheme.Light); }

    public bool UseDarkTheme { get => _settings.Theme == AppTheme.Dark; set => SetTheme(value, AppTheme.Dark); }

    public string DocumentsFolder
    {
        get => _settings.DocumentsFolder;
        set
        {
            if (_settings.DocumentsFolder == value)
                return;

            _settings.DocumentsFolder = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasDocumentsFolder));
        }
    }

    public bool HasDocumentsFolder => _settings.HasDocumentsFolder;

    public string StandardDocumentsFolder
    {
        get => _settings.StandardDocumentsFolder;
        set
        {
            if (_settings.StandardDocumentsFolder == value)
                return;

            _settings.StandardDocumentsFolder = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasStandardDocumentsFolder));
        }
    }

    public bool HasStandardDocumentsFolder => _settings.StandardDocumentsFolder.Length > 0;

    public bool CreateFolderForNewApplications
    {
        get => _settings.CreateFolderForNewApplications;
        set
        {
            _settings.CreateFolderForNewApplications = value;
            OnPropertyChanged();
        }
    }

    public RelayCommand BrowseDocumentsCommand { get; }

    public RelayCommand BrowseStandardDocumentsCommand { get; }

    public RelayCommand ClearDocumentsCommand { get; }

    public RelayCommand ClearStandardDocumentsCommand { get; }

    /// <summary>One document per line, as "Name: words in the file name".</summary>
    public string ChecklistText { get => _checklistText; set => SetProperty(ref _checklistText, value); }

    public RelayCommand ResetChecklistCommand { get; }

    public RelayCommand SaveCommand { get; }

    private string? Pick(string title, string current) =>
        _dialogs.PickFolder(title, current.Length > 0 ? current : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

    private void SetTheme(bool selected, AppTheme theme)
    {
        if (!selected || _settings.Theme == theme)
            return;

        _settings.Theme = theme;
        OnPropertyChanged(nameof(UseSystemTheme));
        OnPropertyChanged(nameof(UseLightTheme));
        OnPropertyChanged(nameof(UseDarkTheme));
    }

    private void Save()
    {
        var settings = _settings.Clone();
        settings.Checklist = DocumentChecklist.Parse(ChecklistText);
        if (_save(settings))
            CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
