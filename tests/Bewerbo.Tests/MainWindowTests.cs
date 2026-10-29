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
        store.Save(SampleApplications());

        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Bewerbo;component/Themes/Controls.xaml"),
        });

        var dialogs = new FakeDialogService();
        var window = new MainWindow();
        var main = new MainViewModel(store, new FaviconCache(Path.Combine(folder, "icons")), dialogs);
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
