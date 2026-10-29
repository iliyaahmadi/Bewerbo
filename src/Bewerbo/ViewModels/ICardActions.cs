using Bewerbo.Core;

namespace Bewerbo.ViewModels;

/// <summary>What a card can ask the main view model to do.</summary>
public interface ICardActions
{
    bool ChangeStatus(JobApplication application, ApplicationStatus status);

    void Edit(JobApplication application);

    void OpenLink(JobApplication application);

    void OpenFolder(JobApplication application);

    void AddFiles(JobApplication application, IReadOnlyList<string> files);
}
