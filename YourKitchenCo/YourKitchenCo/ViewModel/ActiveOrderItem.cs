using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YourKitchenCo.Models;

namespace YourKitchenCo.ViewModel;

/// <summary>
/// One row on the Active Orders screen: the Order itself plus the
/// expanded/collapsed state of its card. Order is a plain model with no
/// change notification, so the toggle lives here rather than on it —
/// tapping the card header flips IsExpanded and the tracking details
/// (4-stage timeline, delivery info, notes, totals) show or hide.
/// </summary>
public partial class ActiveOrderItem : ObservableObject
{
    public Order Order { get; }

    [ObservableProperty]
    private bool _isExpanded;

    public ActiveOrderItem(Order order, bool isExpanded = false)
    {
        Order = order;
        _isExpanded = isExpanded;
    }

    /// <summary>"▾" when open, "▸" when closed — the chevron on the card header.</summary>
    public string Chevron => IsExpanded ? "▾" : "▸";

    /// <summary>Delivery floor/location line, or a neutral fallback when the order has none recorded.</summary>
    public string DeliveryLine =>
        string.IsNullOrWhiteSpace(Order.DeliveryFloor) ? "Delivery details on file" : $"Deliver to: {Order.DeliveryFloor}";

    public bool HasNotes => !string.IsNullOrWhiteSpace(Order.SummaryText);

    partial void OnIsExpandedChanged(bool value) => OnPropertyChanged(nameof(Chevron));

    [RelayCommand]
    private void Toggle() => IsExpanded = !IsExpanded;
}
