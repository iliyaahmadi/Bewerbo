using Bewerbo.Core.Storage;

namespace Bewerbo.Core.Tests;

public class ApplicationStoreTests
{
    [Fact]
    public void Save_then_load_keeps_every_field_including_unicode()
    {
        using var folder = new TempFolder();
        var store = new ApplicationStore(folder.File("applications.json"));
        var original = TestData.Application(company: "Müller & Söhne");
        original.Notes = "DNS\nRouter & \"VM\" فارسی";
        original.Location = "Berlin";
        original.AppliedOn = new DateOnly(2026, 9, 1);
        original.Link = "https://example.com/jobs";
        original.Folder = @"C:\Users\Example\Documents\Bewerbungen\Müller & Söhne";

        store.Save([original]);
        var loaded = Assert.Single(store.Load());

        Assert.Equal(original.Company, loaded.Company);
        Assert.Equal(original.Notes, loaded.Notes);
        Assert.Equal(original.AppliedOn, loaded.AppliedOn);
        Assert.Equal(original.Folder, loaded.Folder);
        Assert.Null(loaded.FollowUpOn);
        Assert.Equal(original.Created, loaded.Created);
        Assert.Equal(DateTimeKind.Utc, loaded.Created.Kind);
    }

    [Fact]
    public void Load_returns_an_empty_list_when_there_is_no_file()
    {
        using var folder = new TempFolder();
        Assert.Empty(new ApplicationStore(folder.File("missing.json")).Load());
    }

    [Fact]
    public void Save_keeps_the_previous_version_as_a_backup()
    {
        using var folder = new TempFolder();
        var store = new ApplicationStore(folder.File("applications.json"));
        var application = TestData.Application(status: ApplicationStatus.Applied);

        store.Save([application]);
        store.Save([application.WithStatus(ApplicationStatus.Interviewing, TestData.Now, TestData.Today)]);

        Assert.Equal(ApplicationStatus.Interviewing, Assert.Single(store.Load()).Status);
        var backup = ApplicationStore.Deserialize(File.ReadAllText(store.BackupPath));
        Assert.Equal(ApplicationStatus.Applied, Assert.Single(backup).Status);
    }

    [Fact]
    public void Reads_files_written_by_the_original_version()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "version1-backup.json"));

        var applications = ApplicationStore.Deserialize(json);

        Assert.Equal(2, applications.Count);
        Assert.Equal(ApplicationType.Ausbildung, applications[0].Type);
        Assert.Equal(ApplicationStatus.Saved, applications[0].Status);
        Assert.Equal("", applications[0].Location);
        Assert.Null(applications[0].AppliedOn);
        Assert.Equal("", applications[0].Folder);
        Assert.Equal(new DateTime(2026, 9, 6, 10, 0, 0, DateTimeKind.Utc), applications[0].Created);
    }

    [Fact]
    public void Writes_empty_dates_as_empty_strings_like_version_1()
    {
        var json = ApplicationStore.Serialize([TestData.Application()]);

        Assert.Contains("\"Version\": 1", json);
        Assert.Contains("\"FollowUpOn\": \"\"", json);
        Assert.Contains("\"Type\": \"Ausbildung\"", json);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("not json")]
    [InlineData("""{"Version":2,"Applications":[]}""")]
    [InlineData("""{"Version":1,"Applications":[{"Id":"1","Company":"","Position":"x","Type":"Job","Status":"Saved"}]}""")]
    [InlineData("""{"Version":1,"Applications":[{"Id":"1","Company":"a","Position":"x","Type":"Internship","Status":"Saved"}]}""")]
    [InlineData("""{"Version":1,"Applications":[{"Id":"1","Company":"a","Position":"x","Type":"Job","Status":"Saved","AppliedOn":"2026-02-30"}]}""")]
    [InlineData("""{"Version":1,"Applications":[{"Id":"1","Company":"a","Position":"x","Type":"Job","Status":"Saved"},{"Id":"1","Company":"b","Position":"y","Type":"Job","Status":"Saved"}]}""")]
    public void Rejects_invalid_backups(string json) =>
        Assert.Throws<InvalidDataException>(() => ApplicationStore.Deserialize(json));

    [Fact]
    public void Merge_adds_only_new_applications()
    {
        var existing = TestData.Application(company: "Kept");
        var duplicate = existing.Clone();
        duplicate.Company = "Ignored";
        var fresh = TestData.Application(company: "New");

        var (merged, added) = ApplicationStore.Merge([existing], [duplicate, fresh]);

        Assert.Equal(1, added);
        Assert.Equal(["Kept", "New"], merged.Select(a => a.Company));
    }
}
