namespace Bewerbo.Core;

public sealed class JobApplication
{
    public string Id { get; set; } = "";
    public string Company { get; set; } = "";
    public string Position { get; set; } = "";
    public ApplicationType Type { get; set; }
    public ApplicationStatus Status { get; set; }
    public string Location { get; set; } = "";
    public DateOnly? AppliedOn { get; set; }
    public DateOnly? FollowUpOn { get; set; }
    public string Salary { get; set; } = "";
    public string Link { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }

    public bool IsFollowUpDue(DateOnly today) =>
        FollowUpOn is { } date && date <= today && Status.NeedsFollowUp();

    public JobApplication Clone() => (JobApplication)MemberwiseClone();

    public JobApplication WithStatus(ApplicationStatus status, DateTime utcNow, DateOnly today)
    {
        var copy = Clone();
        copy.Status = status;
        copy.Updated = utcNow;
        if (status == ApplicationStatus.Applied && copy.AppliedOn is null)
            copy.AppliedOn = today;
        return copy;
    }
}
