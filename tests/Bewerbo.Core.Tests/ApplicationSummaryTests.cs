namespace Bewerbo.Core.Tests;

public class ApplicationSummaryTests
{
    [Fact]
    public void Remaining_excludes_rejected_and_withdrawn_applications()
    {
        var due = TestData.Application(status: ApplicationStatus.Applied);
        due.FollowUpOn = TestData.Today;
        var rejectedButDue = TestData.Application(status: ApplicationStatus.Rejected);
        rejectedButDue.FollowUpOn = TestData.Today.AddDays(-3);
        var later = TestData.Application(type: ApplicationType.Job, status: ApplicationStatus.Interviewing);
        later.FollowUpOn = TestData.Today.AddDays(1);

        var summary = ApplicationSummary.From(
        [
            due,
            rejectedButDue,
            later,
            TestData.Application(type: ApplicationType.Job, status: ApplicationStatus.Withdrawn),
            TestData.Application(type: ApplicationType.Job, status: ApplicationStatus.Accepted),
        ], TestData.Today);

        Assert.Equal(5, summary.Total);
        Assert.Equal(3, summary.Remaining);
        Assert.Equal(1, summary.RemainingAusbildung);
        Assert.Equal(2, summary.RemainingJobs);
        Assert.Equal(1, summary.Interviewing);
        Assert.Equal(1, summary.OffersOrAccepted);
        Assert.Equal(1, summary.FollowUpsDue);
        Assert.Equal(1, summary.Rejected);
        Assert.Equal(1, summary.Withdrawn);
    }

    [Fact]
    public void Moving_to_applied_sets_the_applied_date_once()
    {
        var saved = TestData.Application(status: ApplicationStatus.Saved);

        var applied = saved.WithStatus(ApplicationStatus.Applied, TestData.Now, TestData.Today);
        var again = applied.WithStatus(ApplicationStatus.Applied, TestData.Now, TestData.Today.AddDays(5));

        Assert.Equal(TestData.Today, applied.AppliedOn);
        Assert.Equal(TestData.Today, again.AppliedOn);
        Assert.Equal(ApplicationStatus.Saved, saved.Status);
    }
}
