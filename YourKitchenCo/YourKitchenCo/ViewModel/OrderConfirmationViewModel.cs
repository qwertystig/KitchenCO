using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using YourKitchenCo.Models;

namespace YourKitchenCo.ViewModel;

public partial class OrderConfirmationViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty]
    private Order? _firstOrder;

    [ObservableProperty]
    private decimal _totalPaid;

    [ObservableProperty]
    private int _itemCount;

    public ObservableCollection<Order> Orders { get; } = new();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Orders", out var ordersObj) && ordersObj is List<Order> orders)
        {
            Orders.Clear();
            foreach (var order in orders)
                Orders.Add(order);

            FirstOrder = orders.FirstOrDefault();
            TotalPaid = orders.Sum(o => o.TotalAmount);
            ItemCount = orders.Count;
        }
    }

    [RelayCommand]
    private async Task TrackOrderAsync() => await Shell.Current.GoToAsync("//activeorders");

    [RelayCommand]
    private async Task ViewInvoiceAsync()
    {
        if (FirstOrder is null) return;
        var navigationParameter = new Dictionary<string, object> { { "Order", FirstOrder } };
        await Shell.Current.GoToAsync(nameof(Views.TaxInvoicePage), navigationParameter);
    }

    [RelayCommand]
    private async Task DoneAsync() => await Shell.Current.GoToAsync("//userdashboard");
}
