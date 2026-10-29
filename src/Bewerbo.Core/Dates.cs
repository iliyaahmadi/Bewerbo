using System.Globalization;

namespace Bewerbo.Core;

/// <summary>Dates are stored and typed as YYYY-MM-DD.</summary>
public static class Dates
{
    public const string Format = "yyyy-MM-dd";

    public static string ToText(DateOnly? date) =>
        date?.ToString(Format, CultureInfo.InvariantCulture) ?? "";

    /// <summary>Blank text is a valid, empty date.</summary>
    public static bool TryParse(string? text, out DateOnly? date)
    {
        date = null;
        if (string.IsNullOrWhiteSpace(text))
            return true;

        if (!DateOnly.TryParseExact(text.Trim(), Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return false;

        date = parsed;
        return true;
    }
}
