namespace Bewerbo.Core.Tests;

public class DetailsParserTests
{
    [Fact]
    public void Reads_markdown_from_a_code_block()
    {
        const string text = """
            Here you go:
            ```
            - **Company:** Siemens AG
            - **Position:** Fachinformatiker Systemintegration
            Type: ausbildung
            Status: Applied
            Location: Berlin
            Applied on: 2026-10-25
            Link: https://jobs.example.com/x?a=1
            Notes: Cover letter v2
            Interview: maybe in November
            ```
            """;

        var details = DetailsParser.Parse(text);

        Assert.Equal("Siemens AG", details.Company);
        Assert.Equal("Fachinformatiker Systemintegration", details.Position);
        Assert.Equal("ausbildung", details.Type);
        Assert.Equal("Berlin", details.Location);
        Assert.Equal("2026-10-25", details.AppliedOn);
        Assert.Equal("https://jobs.example.com/x?a=1", details.Link);
        Assert.Equal("Cover letter v2\nInterview: maybe in November", details.Notes);
        Assert.Null(details.Salary);
        Assert.Null(details.FollowUpOn);
    }

    [Theory]
    [InlineData("Role: IT Support", "IT Support")]
    [InlineData("## Position: Junior .NET Developer", "Junior .NET Developer")]
    [InlineData("position:   `Backend Developer`  ", "Backend Developer")]
    public void Accepts_label_variants(string line, string expected) =>
        Assert.Equal(expected, DetailsParser.Parse(line).Position);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Just a normal sentence without fields.")]
    [InlineData("Company:")]
    public void Returns_empty_details_when_nothing_matches(string? text) =>
        Assert.True(DetailsParser.Parse(text).IsEmpty);
}
