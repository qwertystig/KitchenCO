using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YourKitchenCo.Models;

public partial class Option : ObservableObject
{
    public string Name { get; set; }
    public decimal AdditionalPrice { get; set; }

    [ObservableProperty]
    private bool isSelected;

    // e.g. "Large (+R60.00)" when there's a price, just "Standard" when free —
    // shown instead of Name so the customer sees the cost before selecting.
    public string DisplayName => AdditionalPrice > 0 ? $"{Name} (+R{AdditionalPrice:F2})" : Name;
}