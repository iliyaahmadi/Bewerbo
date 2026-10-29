namespace Bewerbo.Core.Storage;

public static class DataFolder
{
    public const string FileName = "applications.json";

    private static readonly string LocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    public static string Default { get; } = Path.Combine(LocalAppData, "Bewerbo");

    /// <summary>Where the app kept its data before it was renamed from Apply to Bewerbo.</summary>
    public static string Legacy { get; } = Path.Combine(LocalAppData, "ApplyTracker");

    /// <summary>
    /// Copies data from the legacy folder the first time Bewerbo runs. The legacy folder is left
    /// untouched, and nothing happens once the new folder has its own data file.
    /// </summary>
    /// <returns>True when data was copied.</returns>
    public static bool CopyLegacyData(string folder, string legacyFolder)
    {
        var target = Path.Combine(folder, FileName);
        var source = Path.Combine(legacyFolder, FileName);
        if (File.Exists(target) || !File.Exists(source))
            return false;

        Directory.CreateDirectory(folder);
        File.Copy(source, target);
        if (File.Exists(source + ".bak"))
            File.Copy(source + ".bak", target + ".bak", overwrite: true);

        var legacyIcons = Path.Combine(legacyFolder, "icons");
        if (Directory.Exists(legacyIcons))
        {
            var icons = Directory.CreateDirectory(Path.Combine(folder, "icons"));
            foreach (var icon in Directory.EnumerateFiles(legacyIcons))
                File.Copy(icon, Path.Combine(icons.FullName, Path.GetFileName(icon)), overwrite: true);
        }

        return true;
    }
}
