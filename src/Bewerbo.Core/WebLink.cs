namespace Bewerbo.Core;

public static class WebLink
{
    public static bool IsValid(string? link) =>
        Uri.TryCreate(link, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

    /// <summary>Adds https:// when the scheme is missing, so "company.com/jobs" works.</summary>
    public static string Normalize(string? link)
    {
        var trimmed = link?.Trim() ?? "";
        if (trimmed.Length > 0 && !trimmed.Contains("://", StringComparison.Ordinal))
            trimmed = "https://" + trimmed;
        return trimmed;
    }

    public static string? Host(string? link) =>
        IsValid(link) ? new Uri(link!).Host : null;
}
