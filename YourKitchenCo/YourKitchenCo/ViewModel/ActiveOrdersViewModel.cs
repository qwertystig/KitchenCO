using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YourKitchenCo.Models;
using YourKitchenCo.Services;

namespace YourKitchenCo.ViewModel;

/// <summary>The logged-in user's own upcoming orders (DeliveryDate >= today).</summary>
public partial class ActiveOrdersViewModel : ObservableObject
{
    private readonly IOrderService _orderService;
    private readonly ISessionService _session;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private Order? _nextOrder;

    public ObservableCollection<Order> ActiveOrders { get; } = new();

    public ActiveOrdersViewModel(IOrderService orderService, ISessionService session)
    {
        _orderService = orderService;
        _session = session;
        _ = RefreshOrdersAsync();
    }

    [RelayCommand]
    private async Task RefreshOrdersAsync()
    {
        if (!_session.IsLoggedIn) return;

        IsBusy = true;

        var orders = await _orderService.GetActiveOrdersForUserAsync(_session.CurrentUser!.Id);

        ActiveOrders.Clear();
        foreach (var order in orders)
            ActiveOrders.Add(order);

        // Already sorted soonest-first by the service — the delivery
        // tracker shows progress for whichever order is coming up next.
        NextOrder = ActiveOrders.Count > 0 ? ActiveOrders[0] : null;

        IsBusy = false;
    }
}
