namespace Bewerbo.Core;

/// <summary>
/// The numbers in the sidebar overview. "Remaining" counts every application that is still
/// in play, which is everything except rejected and withdrawn ones.
/// </summary>
public sealed record ApplicationSummary(
    int Total,
    int Remaining,
    IReadOnlyList<LabelGroup> RemainingByType,
    int Interviewing,
    int OffersOrAccepted,
    int FollowUpsDue,
    int Rejected,
    int Withdrawn)
{
    public static ApplicationSummary From(IReadOnlyCollection<JobApplication> applications, DateOnly today)
    {
        var remaining = applications.Where(a => !a.Status.IsClosed()).ToList();
        return new ApplicationSummary(
            Total: applications.Count,
            Remaining: remaining.Count,
            RemainingByType: ApplicationTypes.Group(remaining),
            Interviewing: applications.Count(a => a.Status == ApplicationStatus.Interviewing),
            OffersOrAccepted: applications.Count(a => a.Status is ApplicationStatus.Offer or ApplicationStatus.Accepted),
            FollowUpsDue: applications.Count(a => a.IsFollowUpDue(today)),
            Rejected: applications.Count(a => a.Status == ApplicationStatus.Rejected),
            Withdrawn: applications.Count(a => a.Status == ApplicationStatus.Withdrawn));
    }
}
