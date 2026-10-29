using System.Text;
using System.Text.Json;

namespace Bewerbo.Core.Storage;

/// <summary>
/// Keeps all applications in one JSON file. Every save replaces the file atomically
/// and keeps the previous version next to it as a .bak file.
/// </summary>
public sealed class ApplicationStore(string filePath)
{
    public const int FormatVersion = 1;

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public string FilePath { get; } = Path.GetFullPath(filePath);

    public string BackupPath => FilePath + ".bak";

    public string DataFolder => Path.GetDirectoryName(FilePath)!;

    public List<JobApplication> Load() =>
        File.Exists(FilePath) ? Deserialize(File.ReadAllText(FilePath, Encoding.UTF8)) : [];

    public void Save(IEnumerable<JobApplication> applications)
    {
        var json = Serialize(applications);
        Deserialize(json);

        Directory.CreateDirectory(DataFolder);
        var temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, json, Utf8NoBom);

        if (File.Exists(FilePath))
            File.Replace(temporary, FilePath, BackupPath, ignoreMetadataErrors: true);
        else
            File.Move(temporary, FilePath);
    }

    public static string Serialize(IEnumerable<JobApplication> applications) =>
        JsonSerializer.Serialize(new ApplicationDocument(FormatVersion, applications.ToList()), ApplicationJson.Options);

    /// <exception cref="InvalidDataException">The text is not a valid Apply file or backup.</exception>
    public static List<JobApplication> Deserialize(string json)
    {
        ApplicationDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<ApplicationDocument>(json, ApplicationJson.Options);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("This is not a supported Bewerbo backup. " + ex.Message, ex);
        }

        if (document is not { Version: FormatVersion, Applications: { } applications })
            throw new InvalidDataException("This is not a supported Bewerbo backup.");

        var ids = new HashSet<string>();
        foreach (var application in applications)
        {
            if (application is null
                || string.IsNullOrWhiteSpace(application.Id)
                || !ids.Add(application.Id)
                || string.IsNullOrWhiteSpace(application.Company)
                || string.IsNullOrWhiteSpace(application.Position)
                || !Enum.IsDefined(application.Type)
                || !Enum.IsDefined(application.Status))
            {
                throw new InvalidDataException("The backup contains an incomplete or invalid application.");
            }

            application.Location ??= "";
            application.Salary ??= "";
            application.Link ??= "";
            application.Notes ??= "";
        }

        return applications;
    }

    /// <summary>Adds imported applications whose ids are not in the list yet. Existing entries always win.</summary>
    public static (List<JobApplication> Merged, int Added) Merge(IEnumerable<JobApplication> existing, IEnumerable<JobApplication> imported)
    {
        var merged = existing.ToList();
        var ids = merged.Select(a => a.Id).ToHashSet();
        var added = 0;
        foreach (var application in imported)
        {
            if (ids.Add(application.Id))
            {
                merged.Add(application);
                added++;
            }
        }

        return (merged, added);
    }

    private sealed record ApplicationDocument(int Version, List<JobApplication>? Applications);
}
