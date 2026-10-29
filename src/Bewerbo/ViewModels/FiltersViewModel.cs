using Bewerbo.Core;

namespace Bewerbo.ViewModels;

/// <summary>The Advanced filters window.</summary>
public sealed class FiltersViewModel : ObservableObject
{
    private readonly Action<AdvancedFilter> _apply;
    private string _appliedFrom;
    private string _appliedTo;
    private string _location;
    private bool _followUpDueOnly;
    private string _error = "";

    public FiltersViewModel(IReadOnlyList<LabelGroup> positions, AdvancedFilter current, Action<AdvancedFilter> apply)
    {
        _apply = apply;
        Positions = positions.Select(p => new PositionChoice(p, current.PositionKeys.Contains(p.Key))).ToList();
        _appliedFrom = Dates.ToText(current.AppliedFrom);
        _appliedTo = Dates.ToText(current.AppliedTo);
        _location = current.Location;
        _followUpDueOnly = current.FollowUpDueOnly;
        ResetCommand = new RelayCommand(Reset);
        ApplyCommand = new RelayCommand(Apply);
    }

    public event EventHandler? CloseRequested;

    public IReadOnlyList<PositionChoice> Positions { get; }

    public bool HasPositions => Positions.Count > 0;

    public string AppliedFrom { get => _appliedFrom; set => SetProperty(ref _appliedFrom, value); }

    public string AppliedTo { get => _appliedTo; set => SetProperty(ref _appliedTo, value); }

    public string Location { get => _location; set => SetProperty(ref _location, value); }

    public bool FollowUpDueOnly { get => _followUpDueOnly; set => SetProperty(ref _followUpDueOnly, value); }

    public string Error { get => _error; private set => SetProperty(ref _error, value); }

    public RelayCommand ResetCommand { get; }

    public RelayCommand ApplyCommand { get; }

    private void Reset()
    {
        foreach (var position in Positions)
            position.IsSelected = false;
        AppliedFrom = "";
        AppliedTo = "";
        Location = "";
        FollowUpDueOnly = false;
        Error = "";
    }

    private void Apply()
    {
        if (!Dates.TryParse(AppliedFrom, out var from) || !Dates.TryParse(AppliedTo, out var to))
        {
            Error = "Enter dates as YYYY-MM-DD, or leave them empty.";
            return;
        }

        _apply(new AdvancedFilter
        {
            PositionKeys = Positions.Where(p => p.IsSelected).Select(p => p.Key).ToHashSet(),
            AppliedFrom = from,
            AppliedTo = to,
            Location = Location.Trim(),
            FollowUpDueOnly = FollowUpDueOnly,
        });
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}

public sealed class PositionChoice(LabelGroup group, bool isSelected) : ObservableObject
{
    private bool _isSelected = isSelected;

    public string Key { get; } = group.Key;

    public string Name { get; } = group.Name;

    public int Count { get; } = group.Count;

    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
}
