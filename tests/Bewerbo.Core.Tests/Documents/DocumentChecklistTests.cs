using Bewerbo.Core.Documents;

namespace Bewerbo.Core.Tests.Documents;

public class DocumentChecklistTests
{
    [Theory]
    [InlineData("Anschreiben_Siemens.pdf", "Cover letter")]
    [InlineData("Motivationsschreiben.docx", "Cover letter")]
    [InlineData("Cover Letter.pdf", "Cover letter")]
    [InlineData("cover-letter-v2.pdf", "Cover letter")]
    [InlineData("Lebenslauf.pdf", "CV")]
    [InlineData("CV_2026.pdf", "CV")]
    [InlineData("Resume.pdf", "CV")]
    [InlineData("Zeugnisse.pdf", "Certificates")]
    [InlineData("Lebenslauf_und_Zeugnisse.pdf", "CV, Certificates")]
    [InlineData("Stellenanzeige.pdf", "")]
    [InlineData("Letterhead.pdf", "")]
    public void Default_checklist_recognises_common_names(string fileName, string expected) =>
        Assert.Equal(expected, string.Join(", ", DocumentChecklist.Default.Where(item => item.Matches(fileName)).Select(item => item.Name)));

    [Fact]
    public void Parses_and_formats_the_settings_text()
    {
        var items = DocumentChecklist.Parse("Cover letter: Anschreiben, cover letter\r\n\n  Portfolio  \nReferences:\n: nothing");

        Assert.Equal(["Cover letter", "Portfolio", "References"], items.Select(i => i.Name));
        Assert.Equal(["anschreiben", "cover letter"], items[0].Keywords);
        Assert.Equal(["portfolio"], items[1].Keywords);
        Assert.Equal(["references"], items[2].Keywords);
        Assert.True(items[1].Matches("Portfolio_Iliya.pdf"));

        var roundTrip = DocumentChecklist.Parse(DocumentChecklist.ToText(DocumentChecklist.Default));
        Assert.Equal(DocumentChecklist.Default.Select(i => i.Name), roundTrip.Select(i => i.Name));
        Assert.Equal(DocumentChecklist.Default[0].Keywords, roundTrip[0].Keywords);
    }
}
