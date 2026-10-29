using System.Text.Json.Serialization;
using Bewerbo.Core.Documents;

namespace Bewerbo.Core;

public enum AppTheme
{
    System,
    Light,
    Dark,
}

public sealed class Settings
{
    public AppTheme Theme { get; set; }

    /// <summary>Folder that holds one subfolder per company, for example Documents\Bewerbungen. Empty turns the documents features off.</summary>
    public string DocumentsFolder { get; set; } = "";

    /// <summary>Files copied into every new company folder, such as the CV and certificates.</summary>
    public string StandardDocumentsFolder { get; set; } = "";

    public bool CreateFolderForNewApplications { get; set; } = true;

    /// <summary>The documents each card checks the company folder for.</summary>
    public List<ChecklistItem> Checklist { get; set; } = [.. DocumentChecklist.Default];

    [JsonIgnore]
    public bool HasDocumentsFolder => DocumentsFolder.Length > 0;

    public Settings Clone()
    {
        var copy = (Settings)MemberwiseClone();
        copy.Checklist = [.. Checklist];
        return copy;
    }
}
