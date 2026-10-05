using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using YourKitchenCo.Models;
using YourKitchenCo.Services;

namespace YourKitchenCo.ViewModel;

public partial class AdminDashboardViewModel : ObservableObject
{
    private readonly IOrderService _orderService;
    private readonly ICompanyDirectoryService _companyDirectory;
    private readonly IUserDirectoryService _userDirectory;

    private List<AdminOrderRow> _allRows = new();

    [ObservableProperty]
    private decimal _todaysRevenue;

    [ObservableProperty]
    private int _activeOrderCount;

    [ObservableProperty]
    private int _companyCount;

    [ObservableProperty]
    private int _customerCount;

    /// <summary>Sum of every order ever placed (delivered or upcoming), incl. delivery fees.</summary>
    [ObservableProperty]
    private decimal _totalRevenue;

    /// <summary>Orders placed since the 1st of the current month.</summary>
    [ObservableProperty]
    private decimal _monthToDateRevenue;

    [ObservableProperty]
    private string _monthToDateLabel = "Month to Date";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _selectedCompanyFilter = "All Companies";

    [ObservableProperty]
    private string _selectedDateFilter = "All Dates";

    public ObservableCollection<AdminOrderRow> UpcomingOrders { get; } = new();
    public ObservableCollection<string> CompanyFilterOptions { get; } = new() { "All Companies" };
    public ObservableCollection<string> DateFilterOptions { get; } = new() { "All Dates" };

    public AdminDashboardViewModel(IOrderService orderService, ICompanyDirectoryService companyDirectory, IUserDirectoryService userDirectory)
    {
        _orderService = orderService;
        _companyDirectory = companyDirectory;
        _userDirectory = userDirectory;
        _ = RefreshOrdersAsync();
    }

    partial void OnSelectedCompanyFilterChanged(string value) => ApplyFilter();
    partial void OnSelectedDateFilterChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task RefreshOrdersAsync()
    {
        IsBusy = true;
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var activeOrders = await _orderService.GetAllActiveOrdersAsync();

            ActiveOrderCount = activeOrders.Count;
            TodaysRevenue = activeOrders.Where(o => o.DeliveryDate == today).Sum(o => o.TotalAmount);

            var companies = await _companyDirectory.GetCompaniesAsync();
            CompanyCount = companies.Count;
            var companyLookup = companies.ToDictionary(c => c.Id, c => c.Name);

            var users = await _userDirectory.GetUsersAsync();
            CustomerCount = users.Count(u => u.Role == "Customer");

            // Revenue tiles (replaced the company/customer counts, per client
            // feedback): all-time, and month-to-date by order date. Delivery
            // fee is only recorded on the first order of a checkout batch, so
            // summing TotalAmount + DeliveryFee per order doesn't double-count it.
            var allOrders = await _orderService.GetAllOrdersAsync();
            TotalRevenue = allOrders.Sum(o => o.TotalAmount + o.DeliveryFee);
            var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            MonthToDateRevenue = allOrders.Where(o => o.OrderDate >= monthStart).Sum(o => o.TotalAmount + o.DeliveryFee);
            MonthToDateLabel = $"Month to Date ({monthStart:MMMM})";

            var rows = new List<AdminOrderRow>();
            foreach (var order in activeOrders.OrderBy(o => o.DeliveryDate))
            {
                var location = await _companyDirectory.GetLocationAsync(order.LocationId);
                rows.Add(new AdminOrderRow
                {
                    Order = order,
                    CompanyName = companyLookup.TryGetValue(order.CompanyId, out var name) ? name : "Unknown Company",
                    LocationName = location?.Name ?? "Unknown Location"
                });
            }

            _allRows = rows;

            var previousCompanySelection = SelectedCompanyFilter;
            CompanyFilterOptions.Clear();
            CompanyFilterOptions.Add("All Companies");
            foreach (var company in companies)
                CompanyFilterOptions.Add(company.Name);
            SelectedCompanyFilter = CompanyFilterOptions.Contains(previousCompanySelection) ? previousCompanySelection : "All Companies";

            var previousDateSelection = SelectedDateFilter;
            var distinctDates = _allRows.Select(r => r.Order.DeliveryDate).Distinct().OrderBy(d => d).ToList();
            DateFilterOptions.Clear();
            DateFilterOptions.Add("All Dates");
            foreach (var date in distinctDates)
                DateFilterOptions.Add(date.ToString("dddd, dd MMM"));
            SelectedDateFilter = DateFilterOptions.Contains(previousDateSelection) ? previousDateSelection : "All Dates";

            ApplyFilter();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilter()
    {
        var filtered = _allRows.AsEnumerable();

        if (SelectedCompanyFilter != "All Companies")
            filtered = filtered.Where(r => r.CompanyName == SelectedCompanyFilter);

        if (SelectedDateFilter != "All Dates")
            filtered = filtered.Where(r => r.Order.DeliveryDate.ToString("dddd, dd MMM") == SelectedDateFilter);

        UpcomingOrders.Clear();
        foreach (var row in filtered)
            UpcomingOrders.Add(row);
    }

    [RelayCommand]
    private async Task MarkPreparingAsync(AdminOrderRow row) => await SetStatusAsync(row, "Preparing");

    [RelayCommand]
    private async Task MarkOutForDeliveryAsync(AdminOrderRow row) => await SetStatusAsync(row, "Out for Delivery");

    [RelayCommand]
    private async Task MarkDeliveredAsync(AdminOrderRow row) => await SetStatusAsync(row, "Delivered");

    private async Task SetStatusAsync(AdminOrderRow row, string status)
    {
        if (row == null) return;

        row.Order.Status = status;
        await _orderService.UpdateOrderStatusAsync(row.Order.Id, status);
        ApplyFilter();
    }

    /// <summary>
    /// Sends the same status update to every currently-filtered order at
    /// once — the point being that when Company + Date are both narrowed
    /// down, that's every order for one company arriving on one day, so
    /// there's no reason to tap through them individually.
    /// </summary>
    [RelayCommand]
    private async Task BulkUpdateStatusAsync()
    {
        if (UpcomingOrders.Count == 0)
        {
            await AlertService.Instance.ShowAsync("Nothing to Update", "No orders match the current filters.", "OK");
            return;
        }

        string action = await AlertService.Instance.ShowActionSheetAsync(
            $"Update status for all {UpcomingOrders.Count} filtered order(s)?", "Cancel", "Preparing", "Out for Delivery", "Delivered");

        if (string.IsNullOrEmpty(action) || action == "Cancel") return;

        bool confirm = await AlertService.Instance.ShowConfirmAsync(
            "Confirm Bulk Update",
            $"Mark all {UpcomingOrders.Count} order(s) for {SelectedCompanyFilter} on {SelectedDateFilter} as \"{action}\"?",
            "Update All", "Cancel");

        if (!confirm) return;

        foreach (var row in UpcomingOrders.ToList())
        {
            row.Order.Status = action;
            await _orderService.UpdateOrderStatusAsync(row.Order.Id, action);
        }

        ApplyFilter();
        await AlertService.Instance.ShowAsync("Updated", $"{UpcomingOrders.Count} order(s) marked as \"{action}\".", "OK");
    }
}
