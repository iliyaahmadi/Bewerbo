using System.Windows.Input;

namespace Bewerbo.ViewModels;

/// <summary>A sidebar entry or status filter: a label with a count that can be selected.</summary>
public sealed record NavItem(string Label, int Count, bool IsActive, ICommand Command);

public sealed record SortOption(string Label, Bewerbo.Core.SortOrder Order)
{
    public override string ToString() => Label;
}
