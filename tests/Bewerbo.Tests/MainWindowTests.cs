using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Bewerbo.Core;
using Bewerbo.Core.Storage;
using Bewerbo.Services;
using Bewerbo.ViewModels;
using Bewerbo.Views;

namespace Bewerbo.Tests;

/// <summary>
/// Opens the real windows with their styles and walks through the main flows.
/// WPF allows a single Application per process, so everything runs in one test on one STA thread.
/// Screenshots of each step are written to the test output folder.
/// </summary>
public class MainWindowTests
{
    [Fact]
    public void Main_flows_work_in_the_real_windows() => RunOnStaThread(() =>
    {
        var folder = Directory.CreateTempSubdirectory("bewerbo-ui-").FullName;
        var screenshots = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "screenshots")).FullName;
        var store = new ApplicationStore(Path.Combine(folder, DataFolder.FileName));
        var settingsStore = new SettingsStore(Path.Combine(folder, SettingsStore.FileName));
        store.Save(SampleApplications());

        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Bewerbo;component/Themes/Controls.xaml"),
        });
        ThemeManager.Apply(app, AppTheme.Dark);
        InputBehavior.Register();
        WindowFrame.Register();

        var dialogs = new FakeDialogService();
        var window = new MainWindow();
        var main = new MainViewModel(store, settingsStore, new FaviconCache(Path.Combine(folder, "icons")), dialogs, theme => ThemeManager.Apply(app, theme));
        window.DataContext = main;
        window.Show();
        Render(window);
        Snap(window, Path.Combine(screenshots, "main.png"));

        Assert.Equal(4, main.Cards.Count);
        Assert.Equal(3, main.Summary.Remaining);
        Assert.Equal(["All applications", "Ausbildung", "Job"], main.Categories.Select(c => c.Label));
        Assert.Equal("Advanced filters…", main.SortOptions[^1].Label);

        main.Categories[2].Command.Execute(null);
        Render(window);
        Assert.Equal("Job", main.Title);
        Assert.Single(main.Cards);

        main.Cards[0].Status = ApplicationStatus.Offer;
        Render(window);
        Assert.Equal(ApplicationStatus.Offer, store.Load().Single(a => a.Type == "Job").Status);

        // Advanced filters: picking the last sort entry keeps the sort order and opens the filters window.
        main.ClearFiltersCommand.Execute(null);
        var sort = main.Sort;
        dialogs.OnFilters = filters =>
        {
            Assert.Equal(["Fachinformatiker für Systemintegration", "Backend Developer (.NET)"], filters.Positions.Select(p => p.Name));
            var filtersWindow = new FiltersWindow(filters) { Owner = window };
            filtersWindow.Show();
            filters.Positions[0].IsSelected = true;
            filters.AppliedFrom = "not a date";
            Render(filtersWindow);
            filters.ApplyCommand.Execute(null);
            Assert.NotEmpty(filters.Error);
            filters.AppliedFrom = "";
            Snap(filtersWindow, Path.Combine(screenshots, "filters.png"));
            filters.ApplyCommand.Execute(null);
            Assert.False(filtersWindow.IsVisible);
        };
        main.Sort = main.SortOptions[^1];
        Assert.Same(sort, main.Sort);
        Render(window);
        Assert.True(main.HasAdvancedFilters);
        Assert.Equal("1 filter", main.AdvancedFiltersLabel);
        Assert.Equal("Fachinformatiker für Systemintegration", main.Title);
        Assert.Equal(3, main.Cards.Count);
        Snap(window, Path.Combine(screenshots, "main-filtered.png"));
        main.ClearAdvancedCommand.Execute(null);
        Assert.False(main.HasAdvancedFilters);
        Assert.Equal(4, main.Cards.Count);

        main.ClearFiltersCommand.Execute(null);
        main.AddCommand.Execute(null);
        var editor = Assert.IsType<EditorViewModel>(dialogs.LastEditor);
        var editorWindow = new EditorWindow(editor) { Owner = window };
        editorWindow.Show();
        Render(editorWindow);

        editor.SaveCommand.Execute(null);
        Assert.NotEmpty(editor.Error);

        Assert.True(editor.Paste("- **Company:** Example Cloud\n- **Position:** Junior .NET Developer\nType: Job\nLink: example.com/jobs"));
        Render(editorWindow);
        Snap(editorWindow, Path.Combine(screenshots, "editor.png"));
        editor.SaveCommand.Execute(null);
        Render(window);

        Assert.False(editorWindow.IsVisible);
        Assert.Equal(5, store.Load().Count);
        Assert.Equal(5, main.Cards.Count);

        main.Search = "no such company";
        Render(window);
        Assert.False(main.HasResults);
        Snap(window, Path.Combine(screenshots, "no-results.png"));

        main.Search = "";
        Assert.False(main.AddFromText("just some text"));
        Assert.True(main.AddFromText("Company: Pasted GmbH\nPosition: Kaufmann für Büromanagement"));
        Assert.Equal("Pasted GmbH", dialogs.LastEditor!.Company);
        Assert.False(dialogs.LastEditor.HasClipboardDetails);

        // Documents: choose a documents folder that already has company folders, then import them.
        var documents = Directory.CreateDirectory(Path.Combine(folder, "Bewerbungen")).FullName;
        var standard = Directory.CreateDirectory(Path.Combine(documents, "Standard")).FullName;
        File.WriteAllText(Path.Combine(standard, "Lebenslauf.pdf"), "cv");
        File.WriteAllText(Path.Combine(standard, "Zeugnisse.pdf"), "certificates");
        var existing = Directory.CreateDirectory(Path.Combine(documents, "Altes Werk")).FullName;
        File.WriteAllText(Path.Combine(existing, "Anschreiben_Altes_Werk.pdf"), "letter");
        File.SetLastWriteTime(Path.Combine(existing, "Anschreiben_Altes_Werk.pdf"), new DateTime(2026, 9, 14, 10, 0, 0));

        dialogs.OnSettings = settings =>
        {
            var settingsWindow = new SettingsWindow(settings) { Owner = window };
            settingsWindow.Show();
            settings.UseLightTheme = true;
            settings.DocumentsFolder = documents;
            settings.StandardDocumentsFolder = standard;
            Render(settingsWindow);
            Snap(settingsWindow, Path.Combine(screenshots, "settings.png"));
            settings.SaveCommand.Execute(null);
            Assert.False(settingsWindow.IsVisible);
        };
        dialogs.OnFolderImport = import =>
        {
            Assert.Equal("Altes Werk", Assert.Single(import.Folders).Company);
            Assert.Equal("Fachinformatiker für Systemintegration", import.Position);
            var importWindow = new FolderImportWindow(import) { Owner = window };
            importWindow.Show();
            Render(importWindow);
            Snap(importWindow, Path.Combine(screenshots, "import-folders.png"));
            import.CreateCommand.Execute(null);
            Assert.False(importWindow.IsVisible);
        };
        main.SettingsCommand.Execute(null);
        Render(window);

        Assert.False(ThemeManager.IsDark);
        Assert.Equal(AppTheme.Light, settingsStore.Load().Theme);
        Assert.Equal(documents, main.DocumentsFolder);
        var imported = store.Load().Single(a => a.Company == "Altes Werk");
        Assert.Equal(existing, imported.Folder);
        Assert.Equal(new DateOnly(2026, 9, 14), imported.AppliedOn);
        Assert.Equal(["Anschreiben_Altes_Werk.pdf"], Directory.GetFiles(existing).Select(Path.GetFileName));

        // A new application gets its own folder with the user's own CV and the other standard documents.
        var ownCv = Path.Combine(folder, "CV_Neue_Firma.pdf");
        File.WriteAllText(ownCv, "tailored cv");
        main.AddCommand.Execute(null);
        var newEditor = dialogs.LastEditor!;
        newEditor.Paste("Company: Neue Firma\nPosition: Fachinformatiker für Systemintegration\nType: Werkstudent");
        Assert.True(newEditor.HasNewFolderNote);
        Assert.Equal(["Ausbildung", "Job"], newEditor.TypeSuggestions.Select(t => t.Name));
        Assert.Equal(["Lebenslauf.pdf", "Zeugnisse.pdf"], newEditor.Documents!.Files.Select(f => f.Name));
        newEditor.Documents.Add([ownCv]);
        newEditor.Documents.Files.Single(f => f.Name == "Lebenslauf.pdf").RemoveCommand.Execute(null);
        Assert.Equal(["Zeugnisse.pdf", "CV_Neue_Firma.pdf"], newEditor.Documents.Files.Select(f => f.Name));
        var newEditorWindow = new EditorWindow(newEditor) { Owner = window };
        newEditorWindow.Show();
        Render(newEditorWindow);
        Snap(newEditorWindow, Path.Combine(screenshots, "editor-new-documents-light.png"));
        newEditor.SaveCommand.Execute(null);
        var newFolder = Path.Combine(documents, "Neue Firma");
        Assert.Equal(newFolder, store.Load().Single(a => a.Company == "Neue Firma").Folder);
        Assert.Equal(["CV_Neue_Firma.pdf", "Zeugnisse.pdf"], Directory.GetFiles(newFolder).Select(Path.GetFileName).Order());
        Assert.Contains("Werkstudent", main.Categories.Select(c => c.Label));

        // Dropping a file on a card copies it into the company folder.
        var letter = Path.Combine(folder, "Anschreiben.pdf");
        File.WriteAllText(letter, "letter");
        main.Cards.Single(c => c.Company == "Neue Firma").AddFiles([letter]);
        Assert.True(File.Exists(Path.Combine(newFolder, "Anschreiben.pdf")));
        Assert.True(File.Exists(letter));
        Assert.Equal("3 files", main.Cards.Single(c => c.Company == "Neue Firma").FilesText);

        // The files window opens, adds and removes files of an application that has no folder yet.
        var unlinked = main.Cards.Single(c => c.Company == "Grünwerk");
        Assert.Equal("No files", unlinked.FilesText);
        var unlinkedFolder = Path.Combine(documents, "Grünwerk");
        dialogs.OnFiles = files =>
        {
            Assert.True(files.Documents.IsEmpty);
            files.Documents.Add([letter]);
            Assert.Equal(["Anschreiben.pdf", "Lebenslauf.pdf", "Zeugnisse.pdf"], files.Documents.Files.Select(f => f.Name));
            files.Documents.Files.Single(f => f.Name == "Lebenslauf.pdf").RemoveCommand.Execute(null);
            Assert.False(File.Exists(Path.Combine(unlinkedFolder, "Lebenslauf.pdf")));
            files.Documents.OpenFolderCommand.Execute(null);

            var filesWindow = new FilesWindow(files) { Owner = window };
            filesWindow.Show();
            Render(filesWindow);
            Snap(filesWindow, Path.Combine(screenshots, "files-light.png"));
            filesWindow.Close();
        };
        unlinked.FilesCommand.Execute(null);
        Assert.Equal(unlinkedFolder, dialogs.Opened.Last());
        Assert.Equal(unlinkedFolder, store.Load().Single(a => a.Company == "Grünwerk").Folder);
        Assert.Equal("2 files", main.Cards.Single(c => c.Company == "Grünwerk").FilesText);

        main.Edit(store.Load().Single(a => a.Company == "Neue Firma"));
        var documentsEditor = Assert.IsType<EditorViewModel>(dialogs.LastEditor);
        Assert.Equal(3, documentsEditor.Documents!.Files.Count);
        var documentsWindow = new EditorWindow(documentsEditor) { Owner = window };
        documentsWindow.Show();
        Render(documentsWindow);
        Snap(documentsWindow, Path.Combine(screenshots, "editor-documents-light.png"));
        documentsWindow.Close();

        Render(window);
        Snap(window, Path.Combine(screenshots, "main-light.png"));

        Assert.Empty(dialogs.Errors);
        window.Close();
        app.Shutdown();
        Directory.Delete(folder, recursive: true);
    });

    private static List<JobApplication> SampleApplications()
    {
        var created = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
        JobApplication Make(string company, string position, string type, ApplicationStatus status, int day) => new()
        {
            Id = Guid.NewGuid().ToString(),
            Company = company,
            Position = position,
            Type = type,
            Status = status,
            Created = created.AddDays(day),
            Updated = created.AddDays(day),
        };

        var interviewing = Make("Nordlicht IT", "Fachinformatiker für Systemintegration", "Ausbildung", ApplicationStatus.Interviewing, 1);
        interviewing.Location = "Hamburg";
        interviewing.AppliedOn = new DateOnly(2026, 10, 2);
        interviewing.FollowUpOn = DateOnly.FromDateTime(DateTime.Today);
        interviewing.Salary = "€1,200 / month";
        interviewing.Notes = "Video interview on Thursday at 10:00";

        return
        [
            interviewing,
            Make("Grünwerk", "Fachinformatiker für Systemintegration", "Ausbildung", ApplicationStatus.Applied, 2),
            Make("Stadtwerke Nord", "Fachinformatiker für Systemintegration", "Ausbildung", ApplicationStatus.Rejected, 3),
            Make("Studio North", "Backend Developer (.NET)", "Job", ApplicationStatus.Applied, 4),
        ];
    }

    private static void Render(Window window)
    {
        window.UpdateLayout();
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () => frame.Continue = false);
        Dispatcher.PushFrame(frame);
        window.UpdateLayout();
    }

    private static void Snap(Window window, string path)
    {
        var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private static void RunOnStaThread(Action test)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                test();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
            throw new Exception("UI test failed", failure);
    }
}
