namespace Bewerbo.Core;

public sealed record LabelGroup(string Key, string Name, int Count);

/// <summary>Grouping for free-text labels such as positions and application types.</summary>
public static class Labels
{
    /// <summary>Case and extra spaces are ignored, so "Fachinformatiker" and " fachinformatiker" are the same.</summary>
    public static string Key(string text) => Clean(text).ToLowerInvariant();

    public static string Clean(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// Largest group first. Each group is shown with the spelling of its most recently updated application.
    /// Applications with an empty label are left out.
    /// </summary>
    public static List<LabelGroup> Group(IEnumerable<JobApplication> applications, Func<JobApplication, string> label) =>
        applications
            .Where(a => Key(label(a)).Length > 0)
            .GroupBy(a => Key(label(a)))
            .Select(g => new LabelGroup(g.Key, Clean(label(g.MaxBy(a => a.Updated)!)), g.Count()))
            .OrderByDescending(g => g.Count)
            .ThenBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
}

public static class Positions
{
    public static string Key(string position) => Labels.Key(position);

    public static List<LabelGroup> Group(IEnumerable<JobApplication> applications) => Labels.Group(applications, a => a.Position);
}

public static class ApplicationTypes
{
    /// <summary>Offered in the form while there are no applications to take types from.</summary>
    public static readonly IReadOnlyList<string> Suggestions = ["Job", "Ausbildung", "Internship"];

    public static List<LabelGroup> Group(IEnumerable<JobApplication> applications) => Labels.Group(applications, a => a.Type);

    /// <summary>
    /// A colour slot from 0 to 3 for the type's tag. Types are numbered alphabetically,
    /// so the same set of types always gets the same colours.
    /// </summary>
    public static int ColorIndex(string type, IEnumerable<string> allTypes)
    {
        var keys = allTypes.Select(Labels.Key).Where(k => k.Length > 0).Distinct().Order(StringComparer.Ordinal).ToList();
        var index = keys.IndexOf(Labels.Key(type));
        return index < 0 ? 0 : index % 4;
    }
}
