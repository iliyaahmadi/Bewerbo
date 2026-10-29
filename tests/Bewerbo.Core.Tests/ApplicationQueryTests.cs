namespace Bewerbo.Core.Tests;

public class ApplicationQueryTests
{
    private static readonly List<JobApplication> Applications =
    [
        Make("Alpha", "Fachinformatiker Systemintegration", "Ausbildung", ApplicationStatus.Applied, day: 1, notes: "DNS and routing"),
        Make("Beta", "fachinformatiker  systemintegration", "Ausbildung", ApplicationStatus.Rejected, day: 2),
        Make("Gamma", "Backend Developer", "Job", ApplicationStatus.Interviewing, day: 3, location: "Berlin"),
    ];

    private static JobApplication Make(string company, string position, string type, ApplicationStatus status, int day, string notes = "", string location = "")
    {
        var application = TestData.Application(company, position, type, status, new DateTime(2026, 10, day, 0, 0, 0, DateTimeKind.Utc));
        application.Notes = notes;
        application.Location = location;
        return application;
    }

    [Fact]
    public void Newest_comes_first_by_default() =>
        Assert.Equal(["Gamma", "Beta", "Alpha"], Companies(new ApplicationFilter()));

    [Fact]
    public void Filters_by_type_and_status() =>
        Assert.Equal(["Alpha"], Companies(new ApplicationFilter(Labels.Key(" ausbildung"), ApplicationStatus.Applied)));

    [Fact]
    public void Filters_by_position_ignoring_case_and_spaces() =>
        Assert.Equal(["Beta", "Alpha"], Companies(new ApplicationFilter(Advanced: new AdvancedFilter { PositionKeys = new HashSet<string> { Positions.Key("Fachinformatiker Systemintegration") } })));

    [Fact]
    public void Advanced_filter_combines_dates_location_and_follow_ups()
    {
        var early = TestData.Application("Early");
        early.AppliedOn = new DateOnly(2026, 9, 1);
        early.Location = "Hamburg";
        var late = TestData.Application("Late");
        late.AppliedOn = new DateOnly(2026, 10, 20);
        late.Location = "Berlin";
        late.FollowUpOn = TestData.Today;
        var undated = TestData.Application("Undated");

        List<JobApplication> all = [early, late, undated];
        string[] Run(AdvancedFilter advanced) =>
            [.. ApplicationQuery.Run(all, new ApplicationFilter(Advanced: advanced, Today: TestData.Today), SortOrder.Company).Select(a => a.Company)];

        Assert.Equal(["Late"], Run(new AdvancedFilter { AppliedFrom = new DateOnly(2026, 10, 1) }));
        Assert.Equal(["Early"], Run(new AdvancedFilter { AppliedTo = new DateOnly(2026, 9, 30) }));
        Assert.Equal(["Early"], Run(new AdvancedFilter { Location = " hamburg " }));
        Assert.Equal(["Late"], Run(new AdvancedFilter { FollowUpDueOnly = true }));
        Assert.Equal(["Early", "Late", "Undated"], Run(AdvancedFilter.None));
        Assert.Equal(2, new AdvancedFilter { AppliedFrom = TestData.Today, FollowUpDueOnly = true }.Count);
    }

    [Theory]
    [InlineData("dns", "Alpha")]
    [InlineData("BERLIN", "Gamma")]
    [InlineData("rejected", "Beta")]
    public void Searches_notes_location_and_status(string search, string expected) =>
        Assert.Equal([expected], Companies(new ApplicationFilter(Search: search)));

    [Fact]
    public void Sorts_by_company() =>
        Assert.Equal(["Alpha", "Beta", "Gamma"], Companies(new ApplicationFilter(), SortOrder.Company));

    [Fact]
    public void Follow_up_sort_puts_applications_without_a_date_last()
    {
        var withDate = TestData.Application("Dated");
        withDate.FollowUpOn = TestData.Today;

        var sorted = ApplicationQuery.Run([TestData.Application("Undated"), withDate], new ApplicationFilter(), SortOrder.FollowUpDate);

        Assert.Equal(["Dated", "Undated"], sorted.Select(a => a.Company));
    }

    [Fact]
    public void Groups_positions_with_counts()
    {
        var groups = Positions.Group(Applications);

        Assert.Equal(2, groups.Count);
        Assert.Equal(2, groups[0].Count);
        Assert.Equal("fachinformatiker systemintegration", groups[0].Key);
        Assert.Equal("Backend Developer", groups[1].Name);
    }

    private static IEnumerable<string> Companies(ApplicationFilter filter, SortOrder order = SortOrder.Newest) =>
        ApplicationQuery.Run(Applications, filter, order).Select(a => a.Company);
}
