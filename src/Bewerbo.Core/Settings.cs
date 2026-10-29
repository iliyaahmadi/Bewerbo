using System.Text.Json.Serialization;

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

    [JsonIgnore]
    public bool HasDocumentsFolder => DocumentsFolder.Length > 0;

    public Settings Clone() => (Settings)MemberwiseClone();
}
