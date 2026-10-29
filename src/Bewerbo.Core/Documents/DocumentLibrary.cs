namespace Bewerbo.Core.Documents;

/// <summary>
/// The documents folder: one subfolder per company with the files sent to it.
/// Bewerbo only creates folders and copies files in. It never deletes, moves or overwrites a file.
/// </summary>
public sealed class DocumentLibrary(string root, string standardDocumentsFolder = "", IReadOnlyList<ChecklistItem>? checklist = null)
{
    private static readonly HashSet<string> IgnoredFiles = new(StringComparer.OrdinalIgnoreCase) { "desktop.ini", "thumbs.db", ".ds_store" };

    public string Root { get; } = root;

    public string StandardDocumentsFolder { get; } = standardDocumentsFolder;

    public IReadOnlyList<ChecklistItem> Checklist { get; } = checklist ?? DocumentChecklist.Default;

    /// <summary>A folder name for the company that Windows accepts.</summary>
    public static string FolderName(string company)
    {
        var invalid = Path.GetInvalidFileNameChars().Concat(['<', '>', ':', '"', '/', '\\', '|', '?', '*']).ToHashSet();
        var name = new string(company.Select(c => invalid.Contains(c) || char.IsControl(c) ? ' ' : c).ToArray());
        name = string.Join(' ', name.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim(' ', '.');
        return name.Length > 0 ? name : "Unnamed";
    }

    /// <summary>The linked folder, or the company's folder in the library when nothing is linked yet.</summary>
    public string FolderFor(JobApplication application) =>
        application.Folder.Length > 0 ? application.Folder : Path.Combine(Root, FolderName(application.Company));

    public static IReadOnlyList<DocumentFile> List(string folder)
    {
        if (!Directory.Exists(folder))
            return [];

        return new DirectoryInfo(folder)
            .EnumerateFiles()
            .Where(file => !IgnoredFiles.Contains(file.Name)
                && !file.Name.StartsWith("~$", StringComparison.Ordinal)
                && !file.Attributes.HasFlag(FileAttributes.Hidden))
            .OrderBy(file => file.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(file => new DocumentFile(file.FullName, file.Length, file.LastWriteTime))
            .ToList();
    }

    /// <summary>
    /// Creates the folder and copies the standard documents into it. A folder that already exists is left as it is,
    /// so linking an application to a folder made by hand never adds files to it.
    /// </summary>
    /// <param name="ownFiles">Files the user brings for this application, such as their own cover letter.
    /// A standard document is not copied when every checklist item it covers is covered by these files.</param>
    /// <returns>The folder path.</returns>
    public string Prepare(string folder, IReadOnlyCollection<string>? ownFiles = null)
    {
        if (Directory.Exists(folder))
            return folder;

        Directory.CreateDirectory(folder);
        if (StandardDocumentsFolder.Length == 0 || !Directory.Exists(StandardDocumentsFolder))
            return folder;

        var provided = Checklist.Where(item => ownFiles?.Any(file => item.Matches(Path.GetFileName(file))) == true).ToList();
        foreach (var document in List(StandardDocumentsFolder).Where(d => !IsReplacedBy(d, provided)))
        {
            File.Copy(document.Path, Path.Combine(folder, document.Name));
        }

        return folder;
    }

    /// <summary>Copies files into the folder. A file whose name is taken gets a number, like "Cover letter (2).pdf".</summary>
    /// <returns>The paths of the new copies.</returns>
    public static List<string> AddFiles(string folder, IEnumerable<string> files)
    {
        Directory.CreateDirectory(folder);
        var added = new List<string>();
        foreach (var file in files.Where(File.Exists))
        {
            if (string.Equals(Path.GetDirectoryName(Path.GetFullPath(file)), Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                continue;

            var target = FreeName(folder, Path.GetFileName(file));
            File.Copy(file, target);
            added.Add(target);
        }

        return added;
    }

    /// <summary>Company folders in the library that no application points to yet.</summary>
    public IReadOnlyList<FolderCandidate> FindUntracked(IEnumerable<JobApplication> applications)
    {
        if (!Directory.Exists(Root))
            return [];

        var tracked = applications
            .Select(a => Path.GetFullPath(FolderFor(a)).TrimEnd(Path.DirectorySeparatorChar))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new DirectoryInfo(Root)
            .EnumerateDirectories()
            .Where(directory => !directory.Attributes.HasFlag(FileAttributes.Hidden)
                && !tracked.Contains(directory.FullName.TrimEnd(Path.DirectorySeparatorChar))
                && !IsStandardDocumentsFolder(directory.FullName))
            .Select(directory =>
            {
                var files = List(directory.FullName);
                DateOnly? first = files.Count > 0 ? DateOnly.FromDateTime(files.Min(f => f.Modified)) : null;
                return new FolderCandidate(directory.Name, directory.FullName, files.Count, first);
            })
            .OrderBy(candidate => candidate.Company, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>Which checklist items the files cover.</summary>
    public IReadOnlyList<(ChecklistItem Item, bool IsPresent)> Check(IEnumerable<DocumentFile> files)
    {
        var names = files.Select(f => f.Name).ToList();
        return Checklist.Select(item => (item, names.Any(item.Matches))).ToList();
    }

    private bool IsReplacedBy(DocumentFile document, List<ChecklistItem> provided)
    {
        var covers = Checklist.Where(item => item.Matches(document.Name)).ToList();
        return covers.Count > 0 && covers.All(provided.Contains);
    }

    private bool IsStandardDocumentsFolder(string path) =>
        StandardDocumentsFolder.Length > 0
        && string.Equals(Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(StandardDocumentsFolder).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);

    private static string FreeName(string folder, string fileName)
    {
        var target = Path.Combine(folder, fileName);
        var stem = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        for (var number = 2; File.Exists(target); number++)
            target = Path.Combine(folder, $"{stem} ({number}){extension}");
        return target;
    }
}
