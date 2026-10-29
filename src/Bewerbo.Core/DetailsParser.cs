namespace Bewerbo.Core;

/// <summary>Fields found in pasted text. Null means the field was not in the text.</summary>
public sealed record ApplicationDetails
{
    public string? Company { get; init; }
    public string? Position { get; init; }
    public string? Type { get; init; }
    public string? Status { get; init; }
    public string? Location { get; init; }
    public string? AppliedOn { get; init; }
    public string? FollowUpOn { get; init; }
    public string? Salary { get; init; }
    public string? Link { get; init; }
    public string? Notes { get; init; }

    public bool IsEmpty => this == new ApplicationDetails();
}

/// <summary>
/// Reads "Field: value" lines from plain text or Markdown, such as a summary written by a chat assistant.
/// Bullets, bold labels and code fences are ignored. Notes can continue over several lines until the next field.
/// </summary>
public static class DetailsParser
{
    /// <summary>Text to give a chat assistant so it answers in the format this parser reads.</summary>
    public const string Prompt = """
        When I tell you I applied somewhere, give me the details in a code block in exactly this format,
        one field per line, and leave out any field you don't know:

        Company:
        Position:
        Type: Ausbildung or Job
        Status: Applied
        Location:
        Applied on: YYYY-MM-DD
        Follow up on: YYYY-MM-DD
        Salary:
        Link:
        Notes:
        """;

    private static readonly Dictionary<string, string> FieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["company"] = nameof(ApplicationDetails.Company),
        ["position"] = nameof(ApplicationDetails.Position),
        ["role"] = nameof(ApplicationDetails.Position),
        ["type"] = nameof(ApplicationDetails.Type),
        ["status"] = nameof(ApplicationDetails.Status),
        ["location"] = nameof(ApplicationDetails.Location),
        ["applied"] = nameof(ApplicationDetails.AppliedOn),
        ["applied on"] = nameof(ApplicationDetails.AppliedOn),
        ["follow up"] = nameof(ApplicationDetails.FollowUpOn),
        ["follow-up"] = nameof(ApplicationDetails.FollowUpOn),
        ["follow up on"] = nameof(ApplicationDetails.FollowUpOn),
        ["follow-up on"] = nameof(ApplicationDetails.FollowUpOn),
        ["salary"] = nameof(ApplicationDetails.Salary),
        ["pay"] = nameof(ApplicationDetails.Salary),
        ["link"] = nameof(ApplicationDetails.Link),
        ["url"] = nameof(ApplicationDetails.Link),
        ["notes"] = nameof(ApplicationDetails.Notes),
    };

    public static ApplicationDetails Parse(string? text)
    {
        var values = new Dictionary<string, string>();
        string? current = null;

        foreach (var rawLine in (text ?? "").ReplaceLineEndings("\n").Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("```", StringComparison.Ordinal))
                continue;

            var clean = line.TrimStart('-', '*', '#', '>', ' ').Replace("**", "").Replace("__", "");
            var colon = clean.IndexOf(':');
            if (colon > 0 && FieldNames.TryGetValue(clean[..colon].Trim(), out var field))
            {
                current = field;
                var value = clean[(colon + 1)..].Trim().Trim('`');
                if (value.Length > 0 || field == nameof(ApplicationDetails.Notes))
                    values[field] = value;
            }
            else if (current == nameof(ApplicationDetails.Notes))
            {
                values[current] = (values[current] + "\n" + rawLine.TrimEnd()).Trim();
            }
        }

        return new ApplicationDetails
        {
            Company = values.GetValueOrDefault(nameof(ApplicationDetails.Company)),
            Position = values.GetValueOrDefault(nameof(ApplicationDetails.Position)),
            Type = values.GetValueOrDefault(nameof(ApplicationDetails.Type)),
            Status = values.GetValueOrDefault(nameof(ApplicationDetails.Status)),
            Location = values.GetValueOrDefault(nameof(ApplicationDetails.Location)),
            AppliedOn = values.GetValueOrDefault(nameof(ApplicationDetails.AppliedOn)),
            FollowUpOn = values.GetValueOrDefault(nameof(ApplicationDetails.FollowUpOn)),
            Salary = values.GetValueOrDefault(nameof(ApplicationDetails.Salary)),
            Link = values.GetValueOrDefault(nameof(ApplicationDetails.Link)),
            Notes = values.GetValueOrDefault(nameof(ApplicationDetails.Notes)),
        };
    }
}
