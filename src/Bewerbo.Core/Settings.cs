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

    /// <summary>The folder used when none is chosen: Documents\Bewerbo.</summary>
    public static string DefaultDocumentsFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Bewerbo");

    /// <summary>Folder that holds one subfolder per company, for example Documents\Bewerbungen. Empty means <see cref="DefaultDocumentsFolder"/>.</summary>
    public string DocumentsFolder { get; set; } = "";

    /// <summary>Files copied into every new company folder, such as the CV and certificates.</summary>
    public string StandardDocumentsFolder { get; set; } = "";

    [JsonIgnore]
    public bool HasDocumentsFolder => DocumentsFolder.Length > 0;

    /// <summary>The chosen documents folder, or the default one.</summary>
    [JsonIgnore]
    public string DocumentsFolderOrDefault => HasDocumentsFolder ? DocumentsFolder : DefaultDocumentsFolder;

    public Settings Clone() => (Settings)MemberwiseClone();
}
