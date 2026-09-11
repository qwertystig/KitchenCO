using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YourKitchenCo.Models;
using YourKitchenCo.Services;
using YourKitchenCo.Views;

namespace YourKitchenCo.ViewModel;

/// <summary>The logged-in user's own past orders (DeliveryDate before today).</summary>
public partial class OrderHistoryViewModel : ObservableObject
{
    private readonly IOrderService _orderService;
    private readonly ISessionService _session;
    private readonly IProductService _productService;
    private readonly ICartService _cartService;
    private readonly IOrderSchedulingService _schedulingService;

    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<Order> OrderHistory { get; } = new();

    public OrderHistoryViewModel(IOrderService orderService, ISessionService session, IProductService productService, ICartService cartService, IOrderSchedulingService schedulingService)
    {
        _orderService = orderService;
        _session = session;
        _productService = productService;
        _cartService = cartService;
        _schedulingService = schedulingService;
        _ = RefreshHistoryAsync();
    }

    [RelayCommand]
    private async Task RefreshHistoryAsync()
    {
        if (!_session.IsLoggedIn) return;

        IsBusy = true;

        var orders = await _orderService.GetOrderHistoryForUserAsync(_session.CurrentUser!.Id);

        OrderHistory.Clear();
        foreach (var order in orders)
            OrderHistory.Add(order);

        IsBusy = false;
    }

    /// <summary>Star tapped on an order — CommandParameter is "OrderId|Position" from the XAML.</summary>
    [RelayCommand]
    private async Task RateStarAsync(string parameter)
    {
        var parts = parameter?.Split('|');
        if (parts is not { Length: 2 }) return;
        if (!int.TryParse(parts[1], out var position)) return;

        var order = OrderHistory.FirstOrDefault(o => o.Id == parts[0]);
        if (order is null) return;

        // Tapping the currently-set star again clears the rating instead of
        // re-setting the same value — a common, forgiving rating-UI pattern.
        var newRating = order.Rating == position ? 0 : position;

        order.Rating = newRating;
        await _orderService.UpdateOrderRatingAsync(order.Id, newRating);

        // Order isn't an ObservableObject, so force the CollectionView to
        // re-read this item the same way other non-observable model updates
        // do elsewhere in this app (see AdminMenuViewModel, AdminCompaniesViewModel).
        var index = OrderHistory.IndexOf(order);
        if (index >= 0)
        {
            OrderHistory.RemoveAt(index);
            OrderHistory.Insert(index, order);
        }

        // Offer optional written feedback right after rating — skippable,
        // matching the reference app's OrderRating.feedback being optional.
        if (newRating > 0)
        {
            var feedback = await AlertService.Instance.ShowPromptAsync(
                "Add a comment? (optional)",
                $"You rated {order.ItemName} {newRating} star{(newRating == 1 ? "" : "s")}.",
                initialValue: order.RatingFeedback);

            if (feedback is not null && feedback != order.RatingFeedback)
            {
                order.RatingFeedback = feedback;
                await _orderService.UpdateOrderRatingFeedbackAsync(order.Id, feedback);
            }
        }
    }

    [RelayCommand]
    private async Task ReportDisputeAsync(Order order)
    {
        if (order is null) return;

        if (order.HasDispute)
        {
            await AlertService.Instance.ShowAsync(
                "Already Reported",
                $"This order already has an open ticket: {order.DisputeTicketRef} ({order.DisputeStatus}).",
                "OK");
            return;
        }

        var reason = await AlertService.Instance.ShowPromptAsync(
            "Report an Issue",
            $"What went wrong with \"{order.ItemName}\"? (e.g. missing item, wrong order, quality issue)");

        if (string.IsNullOrWhiteSpace(reason)) return;

        var ticketRef = await _orderService.ReportDisputeAsync(order.Id, reason);

        order.DisputeReason = reason;
        order.DisputeTicketRef = ticketRef;
        order.DisputeStatus = "Investigating";

        var index = OrderHistory.IndexOf(order);
        if (index >= 0)
        {
            OrderHistory.RemoveAt(index);
            OrderHistory.Insert(index, order);
        }

        await AlertService.Instance.ShowAsync(
            "Issue Reported",
            $"Support ticket {ticketRef} has been opened. We'll follow up shortly.",
            "OK");
    }

    [RelayCommand]
    private async Task ViewInvoiceAsync(Order order)
    {
        if (order is null || string.IsNullOrWhiteSpace(order.TaxInvoiceNumber)) return;

        var navigationParameter = new Dictionary<string, object> { { "Order", order } };
        await Shell.Current.GoToAsync(nameof(TaxInvoicePage), navigationParameter);
    }

    [RelayCommand]
    private async Task ReorderAsync(Order order)
    {
        if (order is null || !order.CanReorder) return;

        // Order only stores ItemName as a display string (e.g. "2x Classic
        // Burger - Beef"), not a reference to the actual Product.
        var (itemName, _) = order.ParseItemNameAndQuantity();

        var products = await _productService.GetProductsAsync("Main");
        var product = products.FirstOrDefault(p => p.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));

        if (product is null)
        {
            await AlertService.Instance.ShowAsync(
                "No Longer Available",
                $"\"{itemName}\" isn't on the current menu anymore, so it can't be reordered directly — take a look at what's available today instead.",
                "OK");
            return;
        }

        var deliveryDate = _schedulingService.GetNextAvailableDeliveryDate();

        var cartItem = new CartItem
        {
            Product = product,
            Quantity = 1,
            DeliveryDate = deliveryDate,
            MenuType = MenuType.Static
        };
        cartItem.RecalculateFinalPrice();

        _cartService.AddItem(cartItem);

        await AlertService.Instance.ShowAsync(
            "Added to Basket",
            $"{product.Name} has been added to your basket for {deliveryDate:dddd, dd MMM}.",
            "Great");
    }
}
