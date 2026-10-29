namespace Bewerbo.Core.Tests;

public class ApplicationDraftTests
{
    [Fact]
    public void New_drafts_are_applied_today()
    {
        var draft = ApplicationDraft.ForNew("Job", TestData.Today);

        Assert.Equal(ApplicationStatus.Applied, draft.Status);
        Assert.Equal("2026-10-29", draft.AppliedOn);
        Assert.Equal("Job", draft.Type);
    }

    [Theory]
    [InlineData("", "Developer", null, null, "company and position")]
    [InlineData("Example", " ", null, null, "company and position")]
    [InlineData("Example", "Developer", "2026-02-30", null, "YYYY-MM-DD")]
    [InlineData("Example", "Developer", null, "29.10.2026", "YYYY-MM-DD")]
    [InlineData("Example", "Developer", null, null, null)]
    public void Validates_required_fields_and_dates(string company, string position, string? appliedOn, string? followUpOn, string? expectedError)
    {
        var draft = new ApplicationDraft { Company = company, Position = position, AppliedOn = appliedOn ?? "", FollowUpOn = followUpOn ?? "" };

        var error = draft.Validate();

        if (expectedError is null)
            Assert.Null(error);
        else
            Assert.Contains(expectedError, error);
    }

    [Fact]
    public void Build_trims_values_and_completes_the_link()
    {
        var draft = new ApplicationDraft { Company = "  Example  ", Position = "Developer ", Link = "example.com/jobs", Status = ApplicationStatus.Saved };

        var application = draft.Build(original: null, TestData.Now, TestData.Today);

        Assert.Equal("Example", application.Company);
        Assert.Equal("Developer", application.Position);
        Assert.Equal("https://example.com/jobs", application.Link);
        Assert.False(string.IsNullOrEmpty(application.Id));
        Assert.Equal(TestData.Now, application.Created);
        Assert.Null(application.AppliedOn);
    }

    [Fact]
    public void Build_fills_in_today_when_an_applied_application_has_no_date()
    {
        var draft = new ApplicationDraft { Company = "Example", Position = "Developer", Status = ApplicationStatus.Applied };

        Assert.Equal(TestData.Today, draft.Build(null, TestData.Now, TestData.Today).AppliedOn);
    }

    [Fact]
    public void Build_keeps_the_identity_of_an_edited_application()
    {
        var original = TestData.Application(created: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var draft = ApplicationDraft.From(original);
        draft.Status = ApplicationStatus.Offer;

        var edited = draft.Build(original, TestData.Now, TestData.Today);

        Assert.Equal(original.Id, edited.Id);
        Assert.Equal(original.Created, edited.Created);
        Assert.Equal(TestData.Now, edited.Updated);
        Assert.Equal(ApplicationStatus.Applied, original.Status);
    }

    [Fact]
    public void Build_refuses_an_invalid_draft() =>
        Assert.Throws<InvalidOperationException>(() => new ApplicationDraft().Build(null, TestData.Now, TestData.Today));

    [Fact]
    public void Fill_copies_pasted_fields_and_ignores_unknown_statuses()
    {
        var draft = ApplicationDraft.ForNew("Ausbildung", TestData.Today);
        draft.Salary = "kept";

        draft.Fill(DetailsParser.Parse("Company: Example\nType: Werkstudent\nStatus: Dreaming"));

        Assert.Equal("Example", draft.Company);
        Assert.Equal("Werkstudent", draft.Type);
        Assert.Equal(ApplicationStatus.Applied, draft.Status);
        Assert.Equal("kept", draft.Salary);
    }
}
