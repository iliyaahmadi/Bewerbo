namespace Bewerbo.Core;

/// <summary>The editor form: free text as typed, checked and turned into a <see cref="JobApplication"/> on save.</summary>
public sealed class ApplicationDraft
{
    public string Company { get; set; } = "";
    public string Position { get; set; } = "";
    public string Type { get; set; } = "";
    public ApplicationStatus Status { get; set; }
    public string Location { get; set; } = "";
    public string AppliedOn { get; set; } = "";
    public string FollowUpOn { get; set; } = "";
    public string Salary { get; set; } = "";
    public string Link { get; set; } = "";
    public string Notes { get; set; } = "";

    public static ApplicationDraft ForNew(string type, DateOnly today) => new()
    {
        Type = type,
        Status = ApplicationStatus.Applied,
        AppliedOn = Dates.ToText(today),
    };

    public static ApplicationDraft From(JobApplication application) => new()
    {
        Company = application.Company,
        Position = application.Position,
        Type = application.Type,
        Status = application.Status,
        Location = application.Location,
        AppliedOn = Dates.ToText(application.AppliedOn),
        FollowUpOn = Dates.ToText(application.FollowUpOn),
        Salary = application.Salary,
        Link = application.Link,
        Notes = application.Notes,
    };

    /// <summary>Copies every field found in the details. Unknown status names are ignored.</summary>
    public void Fill(ApplicationDetails details)
    {
        Company = details.Company ?? Company;
        Position = details.Position ?? Position;
        Location = details.Location ?? Location;
        AppliedOn = details.AppliedOn ?? AppliedOn;
        FollowUpOn = details.FollowUpOn ?? FollowUpOn;
        Salary = details.Salary ?? Salary;
        Link = details.Link ?? Link;
        Notes = details.Notes ?? Notes;
        Type = details.Type ?? Type;
        if (Enum.TryParse<ApplicationStatus>(details.Status, ignoreCase: true, out var status) && Enum.IsDefined(status))
            Status = status;
    }

    /// <returns>A message for the user, or null when the draft can be saved.</returns>
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Company) || string.IsNullOrWhiteSpace(Position))
            return "Please enter a company and position.";
        if (!Dates.TryParse(AppliedOn, out _) || !Dates.TryParse(FollowUpOn, out _))
            return "Enter a valid date as YYYY-MM-DD, or leave it blank.";

        var link = WebLink.Normalize(Link);
        if (link.Length > 0 && !WebLink.IsValid(link))
            return "Use a valid website link, such as https://company.com/jobs.";

        return null;
    }

    /// <param name="original">The application being edited, or null for a new one.</param>
    public JobApplication Build(JobApplication? original, DateTime utcNow, DateOnly today)
    {
        if (Validate() is { } error)
            throw new InvalidOperationException(error);

        Dates.TryParse(AppliedOn, out var appliedOn);
        Dates.TryParse(FollowUpOn, out var followUpOn);

        var application = original?.Clone() ?? new JobApplication { Id = Guid.NewGuid().ToString(), Created = utcNow };
        application.Company = Company.Trim();
        application.Position = Position.Trim();
        application.Type = Labels.Clean(Type);
        application.Status = Status;
        application.Location = Location.Trim();
        application.AppliedOn = appliedOn ?? (Status == ApplicationStatus.Applied ? today : null);
        application.FollowUpOn = followUpOn;
        application.Salary = Salary.Trim();
        application.Link = WebLink.Normalize(Link);
        application.Notes = Notes.Trim();
        application.Updated = utcNow;
        return application;
    }
}
