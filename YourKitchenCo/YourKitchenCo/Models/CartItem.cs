using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YourKitchenCo.Models;

public partial class CartItem : ObservableObject
{
    public Product Product { get; set; } = new();
    public List<Option> SelectedOptions { get; set; } = new();
    public string SpecialRequests { get; set; } = string.Empty;

    // Distinct from SpecialRequests deliberately — this is a safety-critical
    // chef alert (matching the reference app's allergyNotes), not a general
    // preference. Kept separate so it can be surfaced distinctly wherever
    // it matters: flagged on the tax invoice, and counted separately in the
    // kitchen's prep aggregation so allergy-flagged portions can be handled
    // with appropriate care.
    public string AllergyNotes { get; set; } = string.Empty;
    public bool HasAllergyNotes => !string.IsNullOrWhiteSpace(AllergyNotes);

    // Which day this item should be delivered on, and which menu it came from.
    // Set at "Add to Basket" time from the date picker on ProductDetailPage.
    public DateOnly DeliveryDate { get; set; }
    public MenuType MenuType { get; set; }

    public string DeliveryDateLabel => DeliveryDate == default
        ? string.Empty
        : $"For {DeliveryDate:dddd, dd MMM}";

    [ObservableProperty]
    private int _quantity = 1;

    [ObservableProperty]
    private decimal _finalPrice;

    partial void OnQuantityChanged(int value)
    {
        RecalculateFinalPrice();
    }

    public void RecalculateFinalPrice()
    {
        decimal optionsTotal = SelectedOptions?.Sum(o => o.AdditionalPrice) ?? 0m;
        FinalPrice = ((Product?.BasePrice ?? 0m) + optionsTotal) * Quantity;
    }
}
