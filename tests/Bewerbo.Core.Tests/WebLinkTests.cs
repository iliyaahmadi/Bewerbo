namespace Bewerbo.Core.Tests;

public class WebLinkTests
{
    [Theory]
    [InlineData("example.com/jobs", "https://example.com/jobs")]
    [InlineData("  http://example.com ", "http://example.com")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Normalize_adds_https_when_missing(string? input, string expected) =>
        Assert.Equal(expected, WebLink.Normalize(input));

    [Theory]
    [InlineData("https://jobs.example.com/x", "jobs.example.com")]
    [InlineData("ftp://example.com", null)]
    [InlineData("not a link", null)]
    public void Host_is_only_read_from_web_links(string link, string? expected) =>
        Assert.Equal(expected, WebLink.Host(link));
}
