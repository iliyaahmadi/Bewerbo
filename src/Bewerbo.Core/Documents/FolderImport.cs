namespace Bewerbo.Core.Documents;

public static class FolderImport
{
    /// <summary>
    /// Turns company folders into applications. The application date is the date of the oldest file
    /// in the folder, which is usually when the documents were put together and sent.
    /// </summary>
    public static List<JobApplication> CreateApplications(
        IEnumerable<FolderCandidate> candidates,
        string position,
        string type,
        ApplicationStatus status,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(position))
            throw new ArgumentException("A position is required.", nameof(position));

        return candidates.Select(candidate => new JobApplication
        {
            Id = Guid.NewGuid().ToString(),
            Company = candidate.Company,
            Position = position.Trim(),
            Type = Labels.Clean(type),
            Status = status,
            AppliedOn = candidate.FirstFileDate,
            Folder = candidate.Path,
            Created = utcNow,
            Updated = utcNow,
        }).ToList();
    }
}
