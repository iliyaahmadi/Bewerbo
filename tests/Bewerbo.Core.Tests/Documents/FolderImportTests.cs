using Bewerbo.Core.Documents;

namespace Bewerbo.Core.Tests.Documents;

public class FolderImportTests
{
    [Fact]
    public void Creates_one_linked_application_per_folder()
    {
        var candidates = new[]
        {
            new FolderCandidate("Neu AG", Path.Combine("root", "Neu AG"), 2, new DateOnly(2026, 9, 3)),
            new FolderCandidate("Leer GmbH", Path.Combine("root", "Leer GmbH"), 0, null),
        };

        var applications = FolderImport.CreateApplications(candidates, "  Fachinformatiker ", "Ausbildung", ApplicationStatus.Applied, TestData.Now);

        Assert.Equal(2, applications.Count);
        Assert.Equal("Neu AG", applications[0].Company);
        Assert.Equal("Fachinformatiker", applications[0].Position);
        Assert.Equal(Path.Combine("root", "Neu AG"), applications[0].Folder);
        Assert.Equal(new DateOnly(2026, 9, 3), applications[0].AppliedOn);
        Assert.Null(applications[1].AppliedOn);
        Assert.Equal(TestData.Now, applications[1].Created);
        Assert.NotEqual(applications[0].Id, applications[1].Id);
    }

    [Fact]
    public void Requires_a_position() =>
        Assert.Throws<ArgumentException>(() =>
            FolderImport.CreateApplications([], " ", "Job", ApplicationStatus.Applied, TestData.Now));
}
