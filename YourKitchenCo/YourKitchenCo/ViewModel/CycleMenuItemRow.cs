namespace YourKitchenCo.ViewModel;

/// <summary>One editable dish within the cycle menu admin screen.</summary>
public class CycleMenuItemRow
{
    public string WeekKey { get; set; } = string.Empty;
    public string DayName { get; set; } = string.Empty;
    public string CategoryLabel { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public string DisplayHeader => $"{DayName} — {CategoryLabel}";
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);
    public string DisplayValue => IsEmpty ? "(no dish set — tap Add to fill this slot)" : Value;
    public string EditButtonText => IsEmpty ? "Add" : "Edit";
}
