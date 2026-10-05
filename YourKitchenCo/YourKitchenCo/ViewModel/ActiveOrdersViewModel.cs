using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YourKitchenCo.Models;
using YourKitchenCo.Services;

namespace YourKitchenCo.ViewModel;

/// <summary>
/// The logged-in user's own upcoming orders (DeliveryDate >= today),
/// grouped per delivery date, each as a collapsible card whose expanded
/// state carries the full tracking detail (see ActiveOrderItem).
/// </summary>
public partial class ActiveOrdersViewModel : ObservableObject
{
    private readonly IOrderService _orderService;
    private readonly ISessionService _session;

    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Soonest date first; within a date, in the order the service returned them.</summary>
    public ObservableCollection<OrderDateGroup> GroupedOrders { get; } = new();

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

        // Keep whichever cards the person had open across a refresh, so a
        // pull-to-refresh doesn't snap everything shut on them.
        var previouslyExpanded = GroupedOrders
            .SelectMany(g => g)
            .Where(i => i.IsExpanded)
            .Select(i => i.Order.Id)
            .ToHashSet();

        var groups = orders
            .GroupBy(o => o.DeliveryDate)
            .OrderBy(g => g.Key)
            .Select(g => new OrderDateGroup(
                g.Key,
                g.Select(o => new ActiveOrderItem(o, previouslyExpanded.Contains(o.Id)))))
            .ToList();

        // First load: open the very next order so the tracker is visible
        // straight away without a tap, the way the old "delivery progress"
        // header was.
        if (previouslyExpanded.Count == 0 && groups.Count > 0 && groups[0].Count > 0)
            groups[0][0].IsExpanded = true;

        GroupedOrders.Clear();
        foreach (var group in groups)
            GroupedOrders.Add(group);

        IsBusy = false;
    }
}
