using Bewerbo.Core.Documents;

namespace Bewerbo.Core.Tests.Documents;

public class DocumentLibraryTests
{
    [Theory]
    [InlineData("Siemens AG", "Siemens AG")]
    [InlineData("Müller & Söhne", "Müller & Söhne")]
    [InlineData("A/B: Test?", "A B Test")]
    [InlineData("  Firma GmbH. ", "Firma GmbH")]
    [InlineData("???", "Unnamed")]
    public void Folder_names_are_safe_for_windows(string company, string expected) =>
        Assert.Equal(expected, DocumentLibrary.FolderName(company));

    [Fact]
    public void Uses_the_linked_folder_and_otherwise_the_company_folder()
    {
        var library = new DocumentLibrary(Path.Combine("root", "Bewerbungen"));
        var application = TestData.Application(company: "Nord/Licht");

        Assert.Equal(Path.Combine("root", "Bewerbungen", "Nord Licht"), library.FolderFor(application));

        application.Folder = Path.Combine("elsewhere", "Nordlicht");
        Assert.Equal(application.Folder, library.FolderFor(application));
    }

    [Fact]
    public void Prepare_copies_standard_documents_into_a_new_folder_only()
    {
        using var root = new TempFolder();
        var standard = Directory.CreateDirectory(root.File("Standard")).FullName;
        File.WriteAllText(Path.Combine(standard, "Lebenslauf.pdf"), "cv");
        var library = new DocumentLibrary(root.Path, standard);

        var created = library.Prepare(root.File("Neue Firma"));
        Assert.Equal("cv", File.ReadAllText(Path.Combine(created, "Lebenslauf.pdf")));

        var existing = Directory.CreateDirectory(root.File("Alte Firma")).FullName;
        File.WriteAllText(Path.Combine(existing, "Anschreiben.pdf"), "mine");
        library.Prepare(existing);
        Assert.Equal(["Anschreiben.pdf"], Directory.GetFiles(existing).Select(Path.GetFileName));
    }

    [Fact]
    public void Prepare_skips_standard_documents_the_user_brings_their_own_of()
    {
        using var root = new TempFolder();
        var standard = Directory.CreateDirectory(root.File("Standard")).FullName;
        File.WriteAllText(Path.Combine(standard, "Lebenslauf.pdf"), "standard cv");
        File.WriteAllText(Path.Combine(standard, "Zeugnisse.pdf"), "certificates");
        File.WriteAllText(Path.Combine(standard, "Lebenslauf_und_Zeugnisse.pdf"), "both");
        var library = new DocumentLibrary(root.Path, standard);
        string[] own = [Path.Combine("Downloads", "Anschreiben_Firma.pdf"), Path.Combine("Downloads", "CV_neu.pdf")];

        var folder = library.Prepare(root.File("Firma"), own);

        Assert.Equal(["Lebenslauf_und_Zeugnisse.pdf", "Zeugnisse.pdf"], Directory.GetFiles(folder).Select(Path.GetFileName).Order());
    }

    [Fact]
    public void Prepare_works_without_standard_documents()
    {
        using var root = new TempFolder();
        var folder = new DocumentLibrary(root.Path, root.File("missing")).Prepare(root.File("Firma"));
        Assert.True(Directory.Exists(folder));
        Assert.Empty(Directory.GetFiles(folder));
    }

    [Fact]
    public void AddFiles_copies_without_overwriting_and_keeps_the_originals()
    {
        using var root = new TempFolder();
        var folder = Directory.CreateDirectory(root.File("Firma")).FullName;
        File.WriteAllText(Path.Combine(folder, "Anschreiben.pdf"), "first");
        var source = Directory.CreateDirectory(root.File("Downloads")).FullName;
        File.WriteAllText(Path.Combine(source, "Anschreiben.pdf"), "second");

        var added = DocumentLibrary.AddFiles(folder, [Path.Combine(source, "Anschreiben.pdf"), Path.Combine(source, "missing.pdf")]);

        Assert.Equal([Path.Combine(folder, "Anschreiben (2).pdf")], added);
        Assert.Equal("first", File.ReadAllText(Path.Combine(folder, "Anschreiben.pdf")));
        Assert.Equal("second", File.ReadAllText(Path.Combine(folder, "Anschreiben (2).pdf")));
        Assert.True(File.Exists(Path.Combine(source, "Anschreiben.pdf")));
    }

    [Fact]
    public void AddFiles_skips_files_that_are_already_in_the_folder()
    {
        using var root = new TempFolder();
        var folder = Directory.CreateDirectory(root.File("Firma")).FullName;
        File.WriteAllText(Path.Combine(folder, "Lebenslauf.pdf"), "cv");

        Assert.Empty(DocumentLibrary.AddFiles(folder, [Path.Combine(folder, "Lebenslauf.pdf")]));
        Assert.Single(Directory.GetFiles(folder));
    }

    [Fact]
    public void List_skips_system_and_temporary_files()
    {
        using var root = new TempFolder();
        File.WriteAllText(root.File("Lebenslauf.pdf"), "cv");
        File.WriteAllText(root.File("desktop.ini"), "");
        File.WriteAllText(root.File("~$Anschreiben.docx"), "");

        var file = Assert.Single(DocumentLibrary.List(root.Path));
        Assert.Equal("Lebenslauf.pdf", file.Name);
        Assert.Empty(DocumentLibrary.List(root.File("missing")));
    }

    [Fact]
    public void Check_uses_the_custom_checklist()
    {
        var library = new DocumentLibrary("root", checklist: DocumentChecklist.Parse("Portfolio\nCV: cv"));
        var files = new[] { new DocumentFile(Path.Combine("x", "Portfolio.pdf"), 1, TestData.Now) };

        Assert.Equal([("Portfolio", true), ("CV", false)], library.Check(files).Select(c => (c.Item.Name, c.IsPresent)));
    }

    [Fact]
    public void FindUntracked_lists_company_folders_without_an_application()
    {
        using var root = new TempFolder();
        var standard = Directory.CreateDirectory(root.File("Standard")).FullName;
        Directory.CreateDirectory(root.File("Tracked GmbH"));
        var linked = Directory.CreateDirectory(root.File("Linked")).FullName;
        var untracked = Directory.CreateDirectory(root.File("Neu AG")).FullName;
        File.WriteAllText(Path.Combine(untracked, "Anschreiben.pdf"), "letter");
        File.SetLastWriteTime(Path.Combine(untracked, "Anschreiben.pdf"), new DateTime(2026, 9, 3, 8, 0, 0));

        var linkedApplication = TestData.Application(company: "Somewhere Else");
        linkedApplication.Folder = linked;
        var library = new DocumentLibrary(root.Path, standard);

        var candidate = Assert.Single(library.FindUntracked([TestData.Application(company: "Tracked GmbH"), linkedApplication]));

        Assert.Equal("Neu AG", candidate.Company);
        Assert.Equal(untracked, candidate.Path);
        Assert.Equal(1, candidate.FileCount);
        Assert.Equal(new DateOnly(2026, 9, 3), candidate.FirstFileDate);
        Assert.Empty(new DocumentLibrary(root.File("missing")).FindUntracked([]));
    }
}
