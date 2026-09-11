using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using YourKitchenCo.Models;
using YourKitchenCo.Services;

namespace YourKitchenCo.ViewModel;

public partial class AdminActiveOrdersViewModel : ObservableObject
{
    private readonly IOrderService _orderService;
    private readonly ICompanyDirectoryService _companyDirectory;

    private List<AdminOrderRow> _allRows = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _selectedCompanyFilter = "All Companies";

    [ObservableProperty]
    private string _selectedDateFilter = "All Dates";

    [ObservableProperty]
    private bool _isPrepViewActive;

    public ObservableCollection<AdminOrderRow> Orders { get; } = new();
    public ObservableCollection<string> CompanyFilterOptions { get; } = new() { "All Companies" };
    public ObservableCollection<string> DateFilterOptions { get; } = new() { "All Dates" };
    public ObservableCollection<PrepSummaryItem> PrepSummary { get; } = new();

    public AdminActiveOrdersViewModel(IOrderService orderService, ICompanyDirectoryService companyDirectory)
    {
        _orderService = orderService;
        _companyDirectory = companyDirectory;
        _ = RefreshOrdersAsync();
    }

    partial void OnSelectedCompanyFilterChanged(string value) => ApplyFilter();
    partial void OnSelectedDateFilterChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task RefreshOrdersAsync()
    {
        IsBusy = true;

        var companies = await _companyDirectory.GetCompaniesAsync();
        var companyLookup = companies.ToDictionary(c => c.Id, c => c.Name);

        CompanyFilterOptions.Clear();
        CompanyFilterOptions.Add("All Companies");
        foreach (var company in companies)
            CompanyFilterOptions.Add(company.Name);

        var orders = await _orderService.GetAllActiveOrdersAsync();

        var rows = new List<AdminOrderRow>();
        foreach (var order in orders)
        {
            var location = await _companyDirectory.GetLocationAsync(order.LocationId);
            rows.Add(new AdminOrderRow
            {
                Order = order,
                CompanyName = companyLookup.TryGetValue(order.CompanyId, out var name) ? name : "Unknown Company",
                LocationName = location?.Name ?? "Unknown Location"
            });
        }

        _allRows = rows
            .OrderBy(r => r.Order.DeliveryDate)
            .ThenBy(r => r.CompanyName)
            .ThenBy(r => r.LocationName)
            .ToList();

        // Date filter options are built from whatever dates actually have
        // orders right now, in delivery order, rather than a fixed list.
        var distinctDates = _allRows
            .Select(r => r.Order.DeliveryDate)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        var previousDateSelection = SelectedDateFilter;
        DateFilterOptions.Clear();
        DateFilterOptions.Add("All Dates");
        foreach (var date in distinctDates)
            DateFilterOptions.Add(date.ToString("dddd, dd MMM"));

        SelectedDateFilter = DateFilterOptions.Contains(previousDateSelection) ? previousDateSelection : "All Dates";

        ApplyFilter();

        IsBusy = false;
    }

    private void ApplyFilter()
    {
        var filtered = _allRows.AsEnumerable();

        if (SelectedCompanyFilter != "All Companies")
            filtered = filtered.Where(r => r.CompanyName == SelectedCompanyFilter);

        if (SelectedDateFilter != "All Dates")
            filtered = filtered.Where(r => r.Order.DeliveryDate.ToString("dddd, dd MMM") == SelectedDateFilter);

        var filteredList = filtered.ToList();

        Orders.Clear();
        foreach (var row in filteredList)
            Orders.Add(row);

        // Bulk kitchen prep list: same filtered orders, aggregated by dish
        // name instead of listed per-order — "make 12 Chicken Curry, 8 Beef
        // Stir-fry" rather than reading through every individual order.
        var prepTotals = filteredList
            .Select(r => r.Order.ParseItemNameAndQuantity())
            .GroupBy(p => p.DishName)
            .Select(g => new PrepSummaryItem { DishName = g.Key, TotalQuantity = g.Sum(p => p.Quantity) })
            .OrderByDescending(p => p.TotalQuantity)
            .ToList();

        PrepSummary.Clear();
        foreach (var item in prepTotals)
            PrepSummary.Add(item);
    }

    [RelayCommand]
    private void ToggleView() => IsPrepViewActive = !IsPrepViewActive;

    [RelayCommand]
    private async Task UpdateStatusAsync(AdminOrderRow row)
    {
        if (row == null || Application.Current?.MainPage == null) return;

        string action = await AlertService.Instance.ShowActionSheetAsync(
            $"Update Status for Order #{row.Order.OrderNumber}", "Cancel", "Received", "Preparing", "Out for Delivery", "Delivered");

        if (!string.IsNullOrEmpty(action) && action != "Cancel")
        {
            row.Order.Status = action;
            await _orderService.UpdateOrderStatusAsync(row.Order.Id, action);
            ApplyFilter(); // refresh bindings
        }
    }

    /// <summary>
    /// Builds a plain-text order sheet for whatever's currently filtered
    /// (by company and/or date) and hands it to the OS share sheet, where
    /// "Print" is one of the standard options on both iOS and Android —
    /// there's no MAUI-native print API, so this is the practical
    /// zero-dependency way to get a physical order sheet.
    /// </summary>
    [RelayCommand]
    private async Task PrintOrderSheetAsync()
    {
        if (Orders.Count == 0)
        {
            await AlertService.Instance.ShowAsync("Nothing to Print", "There are no orders matching the current filters.", "OK");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("YOUR KITCHEN CO. — ORDER SHEET");
        sb.AppendLine($"Company: {SelectedCompanyFilter}    Date: {SelectedDateFilter}");
        sb.AppendLine($"Generated: {DateTime.Now:dddd, dd MMM yyyy HH:mm}");
        sb.AppendLine(new string('-', 42));

        foreach (var group in Orders.GroupBy(r => (r.CompanyName, r.LocationName)).OrderBy(g => g.Key.CompanyName).ThenBy(g => g.Key.LocationName))
        {
            sb.AppendLine();
            sb.AppendLine($"{group.Key.CompanyName} — {group.Key.LocationName}");

            foreach (var row in group.OrderBy(r => r.Order.DeliveryDate))
            {
                sb.AppendLine($"  [{row.DeliveryDateLabel}] #{row.Order.OrderNumber} — {row.Order.CustomerName}: {row.Order.ItemName} (R{row.Order.TotalAmount:F2}) — {row.Order.Status}");
            }
        }

        sb.AppendLine();
        sb.AppendLine(new string('-', 42));
        sb.AppendLine($"Total orders: {Orders.Count}");

        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Text = sb.ToString(),
            Title = "Order Sheet"
        });
    }

    /// <summary>Same idea as PrintOrderSheetAsync, but for the aggregated kitchen prep list instead of individual orders.</summary>
    [RelayCommand]
    private async Task PrintPrepSheetAsync()
    {
        if (PrepSummary.Count == 0)
        {
            await AlertService.Instance.ShowAsync("Nothing to Print", "There are no orders matching the current filters.", "OK");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("YOUR KITCHEN CO. — KITCHEN PREP SHEET");
        sb.AppendLine($"Company: {SelectedCompanyFilter}    Date: {SelectedDateFilter}");
        sb.AppendLine($"Generated: {DateTime.Now:dddd, dd MMM yyyy HH:mm}");
        sb.AppendLine(new string('-', 42));
        sb.AppendLine();

        foreach (var item in PrepSummary)
            sb.AppendLine($"  {item.TotalQuantity,3}x  {item.DishName}");

        sb.AppendLine();
        sb.AppendLine(new string('-', 42));
        sb.AppendLine($"Total meals: {PrepSummary.Sum(p => p.TotalQuantity)}   Distinct dishes: {PrepSummary.Count}");

        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Text = sb.ToString(),
            Title = "Kitchen Prep Sheet"
        });
    }
}
