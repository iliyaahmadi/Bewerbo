namespace Bewerbo.Core.Documents;

[Flags]
public enum DocumentKinds
{
    None = 0,
    CoverLetter = 1,
    Resume = 2,
    Certificates = 4,
    Photo = 8,

    /// <summary>The three documents almost every application needs.</summary>
    Essentials = CoverLetter | Resume | Certificates,
}

/// <summary>Recognises German and English document names, such as Anschreiben_Name.pdf or Lebenslauf_und_Zeugnisse.pdf.</summary>
public static class DocumentClassifier
{
    private static readonly (DocumentKinds Kind, string[] Words)[] Rules =
    [
        (DocumentKinds.CoverLetter, ["anschreiben", "motivationsschreiben", "cover", "letter"]),
        (DocumentKinds.Resume, ["lebenslauf", "resume", "cv"]),
        (DocumentKinds.Certificates, ["zeugnis", "nachweis", "zertifikat", "certificate", "abschluss", "unterlagen", "dokumente"]),
        (DocumentKinds.Photo, ["foto", "photo", "bewerbungsbild", "bewerbungsfoto"]),
    ];

    public static DocumentKinds Classify(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
        var words = name.Split(['_', '-', ' ', '.'], StringSplitOptions.RemoveEmptyEntries);

        // "Bewerbung_Name.pdf" is usually the complete application in one file.
        if (words.Contains("bewerbung") || words.Contains("bewerbungsunterlagen"))
            return DocumentKinds.Essentials;

        var kinds = DocumentKinds.None;
        foreach (var (kind, keywords) in Rules)
        {
            if (keywords.Any(keyword => words.Any(word => word.StartsWith(keyword, StringComparison.Ordinal))))
                kinds |= kind;
        }

        return kinds;
    }
}
