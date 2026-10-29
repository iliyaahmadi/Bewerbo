using Bewerbo.Core.Storage;

namespace Bewerbo.Core.Tests;

public class DataFolderTests
{
    [Fact]
    public void Copies_legacy_data_once_and_leaves_the_legacy_folder_alone()
    {
        using var root = new TempFolder();
        var legacy = Directory.CreateDirectory(root.File("ApplyTracker")).FullName;
        Directory.CreateDirectory(Path.Combine(legacy, "icons"));
        File.WriteAllText(Path.Combine(legacy, DataFolder.FileName), "current");
        File.WriteAllText(Path.Combine(legacy, DataFolder.FileName + ".bak"), "previous");
        File.WriteAllText(Path.Combine(legacy, "icons", "example.com.png"), "icon");
        var folder = root.File("Bewerbo");

        Assert.True(DataFolder.CopyLegacyData(folder, legacy));
        Assert.Equal("current", File.ReadAllText(Path.Combine(folder, DataFolder.FileName)));
        Assert.Equal("previous", File.ReadAllText(Path.Combine(folder, DataFolder.FileName + ".bak")));
        Assert.True(File.Exists(Path.Combine(folder, "icons", "example.com.png")));
        Assert.True(File.Exists(Path.Combine(legacy, DataFolder.FileName)));

        File.WriteAllText(Path.Combine(legacy, DataFolder.FileName), "changed later");
        Assert.False(DataFolder.CopyLegacyData(folder, legacy));
        Assert.Equal("current", File.ReadAllText(Path.Combine(folder, DataFolder.FileName)));
    }

    [Fact]
    public void Does_nothing_without_legacy_data()
    {
        using var root = new TempFolder();
        Assert.False(DataFolder.CopyLegacyData(root.File("Bewerbo"), root.File("ApplyTracker")));
        Assert.False(Directory.Exists(root.File("Bewerbo")));
    }
}
