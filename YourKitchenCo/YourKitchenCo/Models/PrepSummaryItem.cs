namespace YourKitchenCo.Models;

/// <summary>One dish's total quantity needed across all filtered orders — the kitchen's bulk prep list, matching the reference app's AggregatedPrepItem.</summary>
public class PrepSummaryItem
{
    public string DishName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
}
