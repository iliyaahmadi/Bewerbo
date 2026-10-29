using Bewerbo.Core.Documents;

namespace Bewerbo.Core.Tests.Documents;

public class DocumentClassifierTests
{
    [Theory]
    [InlineData("Anschreiben_Siemens.pdf", DocumentKinds.CoverLetter)]
    [InlineData("Motivationsschreiben.docx", DocumentKinds.CoverLetter)]
    [InlineData("Cover Letter.pdf", DocumentKinds.CoverLetter)]
    [InlineData("Lebenslauf.pdf", DocumentKinds.Resume)]
    [InlineData("CV_2026.pdf", DocumentKinds.Resume)]
    [InlineData("Zeugnisse.pdf", DocumentKinds.Certificates)]
    [InlineData("Abschlusszeugnis.pdf", DocumentKinds.Certificates)]
    [InlineData("Bewerbungsfoto.jpg", DocumentKinds.Photo)]
    [InlineData("Foto.jpg", DocumentKinds.Photo)]
    [InlineData("Lebenslauf_und_Zeugnisse.pdf", DocumentKinds.Resume | DocumentKinds.Certificates)]
    [InlineData("Bewerbung_Iliya.pdf", DocumentKinds.Essentials)]
    [InlineData("Bewerbungsunterlagen.pdf", DocumentKinds.Essentials)]
    [InlineData("Stellenanzeige.pdf", DocumentKinds.None)]
    public void Recognises_common_document_names(string fileName, DocumentKinds expected) =>
        Assert.Equal(expected, DocumentClassifier.Classify(fileName));
}
