namespace Bewerbo.Core;

public enum ApplicationStatus
{
    Saved,
    Applied,
    Interviewing,
    Offer,
    Accepted,
    Rejected,
    Withdrawn,
}

public static class ApplicationStatusExtensions
{
    /// <summary>Rejected and withdrawn applications are no longer in play.</summary>
    public static bool IsClosed(this ApplicationStatus status) =>
        status is ApplicationStatus.Rejected or ApplicationStatus.Withdrawn;

    public static bool NeedsFollowUp(this ApplicationStatus status) =>
        !status.IsClosed() && status != ApplicationStatus.Accepted;
}
