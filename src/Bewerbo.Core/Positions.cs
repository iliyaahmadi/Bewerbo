namespace Bewerbo.Core;

public sealed record PositionGroup(string Key, string Name, int Count);

public static class Positions
{
    /// <summary>Case and extra spaces are ignored, so "Fachinformatiker" and " fachinformatiker" are the same position.</summary>
    public static string Key(string position) =>
        string.Join(' ', position.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    /// <summary>Each group is shown with the spelling of its most recently updated application.</summary>
    public static List<PositionGroup> Group(IEnumerable<JobApplication> applications) =>
        applications
            .GroupBy(a => Key(a.Position))
            .Select(g => new PositionGroup(g.Key, g.MaxBy(a => a.Updated)!.Position.Trim(), g.Count()))
            .OrderByDescending(g => g.Count)
            .ThenBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
}
