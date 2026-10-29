namespace Bewerbo.Core;

/// <summary>The criteria set in the Advanced filters window. Empty or null values mean "any".</summary>
public sealed record AdvancedFilter
{
    public static readonly AdvancedFilter None = new();

    /// <summary>Position keys (see <see cref="Positions.Key"/>). Empty means every position.</summary>
    public IReadOnlySet<string> PositionKeys { get; init; } = new HashSet<string>();

    public DateOnly? AppliedFrom { get; init; }

    public DateOnly? AppliedTo { get; init; }

    public string Location { get; init; } = "";

    public bool FollowUpDueOnly { get; init; }

    /// <summary>How many criteria are set, for the "Filters · 2" label.</summary>
    public int Count =>
        (PositionKeys.Count > 0 ? 1 : 0)
        + (AppliedFrom is not null || AppliedTo is not null ? 1 : 0)
        + (Location.Trim().Length > 0 ? 1 : 0)
        + (FollowUpDueOnly ? 1 : 0);

    public bool IsEmpty => Count == 0;

    /// <summary>Applications without an application date never match a date range.</summary>
    public bool Matches(JobApplication application, DateOnly today) =>
        (PositionKeys.Count == 0 || PositionKeys.Contains(Positions.Key(application.Position)))
        && (AppliedFrom is null || application.AppliedOn >= AppliedFrom)
        && (AppliedTo is null || application.AppliedOn <= AppliedTo)
        && (Location.Trim().Length == 0 || application.Location.Contains(Location.Trim(), StringComparison.OrdinalIgnoreCase))
        && (!FollowUpDueOnly || application.IsFollowUpDue(today));
}
