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
    string? TypeKey = null,
    ApplicationStatus? Status = null,
    AdvancedFilter? Advanced = null,
    string? Search = null,
    DateOnly Today = default)
{
    public bool Matches(JobApplication application) =>
        (TypeKey is null || Labels.Key(application.Type) == TypeKey)
        && (Status is null || application.Status == Status)
        && (Advanced is null || Advanced.Matches(application, Today))
        && MatchesSearch(application);

    private bool MatchesSearch(JobApplication application)
    {
        var search = Search?.Trim();
        if (string.IsNullOrEmpty(search))
            return true;

        string[] fields = [application.Company, application.Position, application.Location, application.Notes, application.Status.ToString(), application.Type];
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
