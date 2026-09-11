namespace YourKitchenCo.Models;

/// <summary>
/// One selectable delivery date, for binding to the "which day am I ordering
/// for" picker. For the cycle menu, CycleMenu is populated with that day's
/// specific items; for the static menu it's left null since the menu doesn't
/// change day to day.
/// </summary>
public class DeliveryDateOption
{
    public DateOnly Date { get; set; }

    public string DisplayLabel => Date.ToString("dddd, dd MMM");

    public CycleMenuDay? CycleMenu { get; set; }

    public bool IsSelected { get; set; }
}
