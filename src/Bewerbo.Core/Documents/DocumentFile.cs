namespace Bewerbo.Core.Documents;

public sealed record DocumentFile(string Path, long Size, DateTime Modified)
{
    public string Name => System.IO.Path.GetFileName(Path);
}
