namespace Bewerbo.Core.Documents;

/// <summary>A company folder that has no application yet.</summary>
public sealed record FolderCandidate(string Company, string Path, int FileCount, DateOnly? FirstFileDate);
