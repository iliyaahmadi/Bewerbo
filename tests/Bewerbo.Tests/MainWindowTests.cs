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

        var dialogs = new FakeDialogService();
        var window = new MainWindow();
        var main = new MainViewModel(store, settingsStore, new FaviconCache(Path.Combine(folder, "icons")), dialogs, theme => ThemeManager.Apply(app, theme));
        window.DataContext = main;
        window.Show();
        Render(window);
        Snap(window, Path.Combine(screenshots, "main.png"));

        Assert.Equal(4, main.Cards.Count);
        Assert.Equal(3, main.Summary.Remaining);
        Assert.Equal(2, main.Positions.Count);

        main.Categories[2].Command.Execute(null);
        Render(window);
        Assert.Equal("Jobs", main.Title);
        Assert.Single(main.Cards);

        main.Cards[0].Status = ApplicationStatus.Offer;
        Render(window);
        Assert.Equal(ApplicationStatus.Offer, store.Load().Single(a => a.Type == ApplicationType.Job).Status);

        main.ClearFiltersCommand.Execute(null);
        main.Positions[0].Command.Execute(null);
        Render(window);
        Assert.Equal(3, main.Cards.Count);

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
        Assert.True(main.HasDocumentsFolder);
        var imported = store.Load().Single(a => a.Company == "Altes Werk");
        Assert.Equal(existing, imported.Folder);
        Assert.Equal(new DateOnly(2026, 9, 14), imported.AppliedOn);
        Assert.Equal(["Anschreiben_Altes_Werk.pdf"], Directory.GetFiles(existing).Select(Path.GetFileName));

        // A new application gets its own folder with the standard documents.
        main.AddCommand.Execute(null);
        dialogs.LastEditor!.Paste("Company: Neue Firma\nPosition: Fachinformatiker für Systemintegration");
        Assert.True(dialogs.LastEditor.HasNewFolderNote);
        dialogs.LastEditor.SaveCommand.Execute(null);
        var newFolder = Path.Combine(documents, "Neue Firma");
        Assert.Equal(newFolder, store.Load().Single(a => a.Company == "Neue Firma").Folder);
        Assert.True(File.Exists(Path.Combine(newFolder, "Lebenslauf.pdf")));

        // Dropping a file on a card copies it into the company folder.
        var letter = Path.Combine(folder, "Anschreiben.pdf");
        File.WriteAllText(letter, "letter");
        main.Cards.Single(c => c.Company == "Neue Firma").AddFiles([letter]);
        Assert.True(File.Exists(Path.Combine(newFolder, "Anschreiben.pdf")));
        Assert.True(File.Exists(letter));
        var card = main.Cards.Single(c => c.Company == "Neue Firma");
        Assert.All(card.Documents, document => Assert.True(document.IsPresent));
        Assert.Equal("3 files", card.FileCountText);

        // An application without a folder gets one when its folder is opened.
        var unlinked = main.Cards.Single(c => c.Company == "Grünwerk");
        Assert.Equal("No folder yet", unlinked.FileCountText);
        unlinked.OpenFolderCommand.Execute(null);
        Assert.Equal(Path.Combine(documents, "Grünwerk"), dialogs.Opened.Last());
        Assert.Equal(Path.Combine(documents, "Grünwerk"), store.Load().Single(a => a.Company == "Grünwerk").Folder);

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
        JobApplication Make(string company, string position, ApplicationType type, ApplicationStatus status, int day) => new()
        {
            Id = Guid.NewGuid().ToString(),
            Company = company,
            Position = position,
            Type = type,
            Status = status,
            Created = created.AddDays(day),
            Updated = created.AddDays(day),
        };

        var interviewing = Make("Nordlicht IT", "Fachinformatiker für Systemintegration", ApplicationType.Ausbildung, ApplicationStatus.Interviewing, 1);
        interviewing.Location = "Hamburg";
        interviewing.AppliedOn = new DateOnly(2026, 10, 2);
        interviewing.FollowUpOn = DateOnly.FromDateTime(DateTime.Today);
        interviewing.Salary = "€1,200 / month";
        interviewing.Notes = "Video interview on Thursday at 10:00";

        return
        [
            interviewing,
            Make("Grünwerk", "Fachinformatiker für Systemintegration", ApplicationType.Ausbildung, ApplicationStatus.Applied, 2),
            Make("Stadtwerke Nord", "Fachinformatiker für Systemintegration", ApplicationType.Ausbildung, ApplicationStatus.Rejected, 3),
            Make("Studio North", "Backend Developer (.NET)", ApplicationType.Job, ApplicationStatus.Applied, 4),
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
