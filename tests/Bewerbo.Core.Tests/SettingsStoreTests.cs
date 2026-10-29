using Bewerbo.Core.Storage;

namespace Bewerbo.Core.Tests;

public class SettingsStoreTests
{
    [Fact]
    public void Save_then_load_keeps_every_setting()
    {
        using var folder = new TempFolder();
        var store = new SettingsStore(folder.File(SettingsStore.FileName));

        store.Save(new Settings
        {
            Theme = AppTheme.Light,
            DocumentsFolder = @"C:\Users\Example\Documents\Bewerbungen",
            StandardDocumentsFolder = @"C:\Users\Example\Documents\Standard",
        });
        var loaded = store.Load();

        Assert.Equal(AppTheme.Light, loaded.Theme);
        Assert.Equal(@"C:\Users\Example\Documents\Bewerbungen", loaded.DocumentsFolder);
        Assert.Equal(@"C:\Users\Example\Documents\Standard", loaded.StandardDocumentsFolder);
        Assert.Contains("\"Theme\": \"Light\"", File.ReadAllText(store.FilePath));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not json")]
    [InlineData("""{ "Theme": 42, "DocumentsFolder": null }""")]
    public void Missing_or_damaged_files_give_the_defaults(string? content)
    {
        using var folder = new TempFolder();
        var store = new SettingsStore(folder.File(SettingsStore.FileName));
        if (content is not null)
            File.WriteAllText(store.FilePath, content);

        var settings = store.Load();

        Assert.Equal(AppTheme.System, settings.Theme);
        Assert.Equal("", settings.DocumentsFolder);
        Assert.False(settings.HasDocumentsFolder);
    }

    [Fact]
    public void An_empty_documents_folder_means_the_default_one()
    {
        Assert.Equal(Settings.DefaultDocumentsFolder, new Settings().DocumentsFolderOrDefault);
        Assert.EndsWith("Bewerbo", Settings.DefaultDocumentsFolder);
        Assert.Equal("D:\\Jobs", new Settings { DocumentsFolder = "D:\\Jobs" }.DocumentsFolderOrDefault);
    }
}
