namespace Bewerbo.Core.Tests;

internal static class TestData
{
    public static readonly DateOnly Today = new(2026, 10, 29);
    public static readonly DateTime Now = new(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);

    public static JobApplication Application(
        string company = "Example GmbH",
        string position = "Fachinformatiker für Systemintegration",
        string type = "Ausbildung",
        ApplicationStatus status = ApplicationStatus.Applied,
        DateTime? created = null) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Company = company,
        Position = position,
        Type = type,
        Status = status,
        Created = created ?? Now,
        Updated = created ?? Now,
    };
}

internal sealed class TempFolder : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("bewerbo-tests-").FullName;

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
