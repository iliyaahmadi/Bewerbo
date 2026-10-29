namespace Bewerbo.Core.Documents;

/// <summary>A document the user sends with applications, recognised by words in its file name.</summary>
public sealed record ChecklistItem(string Name, IReadOnlyList<string> Keywords)
{
    /// <summary>
    /// True when a word in the file name starts with one of the keywords, so "zeugnis" also finds
    /// "Zeugnisse.pdf". Keywords of several words, such as "cover letter", match them in a row.
    /// </summary>
    public bool Matches(string fileName)
    {
        var words = DocumentChecklist.Words(Path.GetFileNameWithoutExtension(fileName));
        var text = string.Join(' ', words);
        return Keywords
            .Select(keyword => string.Join(' ', DocumentChecklist.Words(keyword)))
            .Where(keyword => keyword.Length > 0)
            .Any(keyword => keyword.Contains(' ')
                ? (" " + text + " ").Contains(" " + keyword, StringComparison.Ordinal)
                : words.Any(word => word.StartsWith(keyword, StringComparison.Ordinal)));
    }
}

/// <summary>The documents each card checks for. Users edit the list in Settings, one item per line.</summary>
public static class DocumentChecklist
{
    public static readonly IReadOnlyList<ChecklistItem> Default =
    [
        new("Cover letter", ["cover letter", "coverletter", "anschreiben", "motivationsschreiben", "motivation letter"]),
        new("CV", ["cv", "resume", "lebenslauf"]),
        new("Certificates", ["certificate", "zeugnis", "zertifikat", "nachweis", "abschluss"]),
    ];

    /// <summary>Formats the checklist as "Name: keyword, keyword" lines for editing.</summary>
    public static string ToText(IEnumerable<ChecklistItem> items) =>
        string.Join(Environment.NewLine, items.Select(item => $"{item.Name}: {string.Join(", ", item.Keywords)}"));

    /// <summary>
    /// Reads "Name: keyword, keyword" lines. A line without a colon uses the name as its only keyword,
    /// so "Portfolio" finds Portfolio.pdf.
    /// </summary>
    public static List<ChecklistItem> Parse(string text) =>
        text.Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .Select(line =>
            {
                var colon = line.IndexOf(':');
                var name = (colon < 0 ? line : line[..colon]).Trim();
                var keywords = colon < 0
                    ? [name.ToLowerInvariant()]
                    : line[(colon + 1)..].Split(',').Select(k => k.Trim().ToLowerInvariant()).Where(k => k.Length > 0).ToList();
                return new ChecklistItem(name, keywords.Count > 0 ? keywords : [name.ToLowerInvariant()]);
            })
            .Where(item => item.Name.Length > 0)
            .ToList();

    /// <summary>Lower-case words, split at spaces, underscores, hyphens and dots.</summary>
    internal static string[] Words(string text) =>
        text.ToLowerInvariant().Split([' ', '_', '-', '.'], StringSplitOptions.RemoveEmptyEntries);
}
