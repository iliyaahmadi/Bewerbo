using System.Text;
using System.Text.Json;

namespace Bewerbo.Core.Storage;

/// <summary>Reads and writes settings.json. A missing or damaged file gives the default settings.</summary>
public sealed class SettingsStore(string filePath)
{
    public const string FileName = "settings.json";

    public string FilePath { get; } = Path.GetFullPath(filePath);

    public Settings Load()
    {
        try
        {
            var settings = File.Exists(FilePath)
                ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath, Encoding.UTF8), ApplicationJson.Options)
                : null;

            if (settings is null)
                return new Settings();

            settings.DocumentsFolder ??= "";
            settings.StandardDocumentsFolder ??= "";
            if (!Enum.IsDefined(settings.Theme))
                settings.Theme = AppTheme.System;
            return settings;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return new Settings();
        }
    }

    public void Save(Settings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings, ApplicationJson.Options), new UTF8Encoding(false));
        File.Move(temporary, FilePath, overwrite: true);
    }
}
