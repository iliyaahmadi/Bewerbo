namespace Bewerbo.Core;

public enum SortOrder
{
    Newest,
    RecentlyUpdated,
    Company,
    FollowUpDate,
}

/// <summary>Null means "any" for every criterion.</summary>
public sealed record ApplicationFilter(
    ApplicationType? Type = null,
    ApplicationStatus? Status = null,
    string? PositionKey = null,
    string? Search = null)
{
    public bool Matches(JobApplication application) =>
        (Type is null || application.Type == Type)
        && (Status is null || application.Status == Status)
        && (PositionKey is null || Positions.Key(application.Position) == PositionKey)
        && MatchesSearch(application);

    private bool MatchesSearch(JobApplication application)
    {
        var search = Search?.Trim();
        if (string.IsNullOrEmpty(search))
            return true;

        string[] fields = [application.Company, application.Position, application.Location, application.Notes, application.Status.ToString(), application.Type.ToString()];
        return fields.Any(field => field.Contains(search, StringComparison.OrdinalIgnoreCase));
    }
}

public static class ApplicationQuery
{
    public static List<JobApplication> Run(IEnumerable<JobApplication> applications, ApplicationFilter filter, SortOrder order)
    {
        var matches = applications.Where(filter.Matches);
        return order switch
        {
            SortOrder.RecentlyUpdated => matches.OrderByDescending(a => a.Updated).ToList(),
            SortOrder.Company => matches.OrderBy(a => a.Company, StringComparer.CurrentCultureIgnoreCase).ToList(),
            SortOrder.FollowUpDate => matches.OrderBy(a => a.FollowUpOn ?? DateOnly.MaxValue).ThenByDescending(a => a.Created).ToList(),
            _ => matches.OrderByDescending(a => a.Created).ToList(),
        };
    }
}
