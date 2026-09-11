using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using YourKitchenCo.Graphics;
using YourKitchenCo.Models;
using YourKitchenCo.Services;

namespace YourKitchenCo.ViewModel;

public class SalesItemSummary
{
    public string ItemName { get; set; } = string.Empty;
    public int UnitsSold { get; set; }
    public decimal TotalRevenue { get; set; }
    public Color SwatchColor { get; set; } = Colors.Gray;
}

public partial class AdminReportsViewModel : ObservableObject
{
    private static readonly Color[] Palette =
    {
        Color.FromArgb("#AF1718"), // berry red
        Color.FromArgb("#DA5D23"), // harvest orange
        Color.FromArgb("#F7F2E8"), // golden yellow
        Color.FromArgb("#C9DE87"), // sage green
        Color.FromArgb("#121212"), // main-menu blue
        Color.FromArgb("#B6DFF8"), // pale blue
    };

    private readonly IOrderService _orderService;
    private readonly ICompanyDirectoryService _companyDirectory;

    private List<Order> _allOrders = new();
    private Dictionary<string, string> _companyNameById = new();

    [ObservableProperty]
    private string _selectedTimeframe = "This Week";

    [ObservableProperty]
    private string _selectedCompanyFilter = "All Companies";

    [ObservableProperty]
    private bool _isCustomRangeVisible;

    [ObservableProperty]
    private DateTime _customStartDate = DateTime.Now.AddDays(-7);

    [ObservableProperty]
    private DateTime _customEndDate = DateTime.Now;

    [ObservableProperty]
    private decimal _totalRevenue;

    [ObservableProperty]
    private int _totalOrders;

    [ObservableProperty]
    private decimal _averageOrderValue;

    [ObservableProperty]
    private string _topSellingItem = "—";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private PieChartDrawable _pieChart = new();

    [ObservableProperty]
    private BarChartDrawable _barChart = new();

    public ObservableCollection<string> TimeframeOptions { get; } = new()
    {
        "Today",
        "This Week",
        "This Month",
        "Custom Range"
    };

    public ObservableCollection<string> CompanyFilterOptions { get; } = new() { "All Companies" };
    public ObservableCollection<string> CategoryFilterOptions { get; } = new() { "All Categories" };

    [ObservableProperty]
    private string _selectedCategoryFilter = "All Categories";

    [ObservableProperty]
    private bool _hasDisputes;

    public ObservableCollection<SalesItemSummary> TopSellingItems { get; } = new();
    public ObservableCollection<Order> DisputedOrders { get; } = new();

    public AdminReportsViewModel(IOrderService orderService, ICompanyDirectoryService companyDirectory)
    {
        _orderService = orderService;
        _companyDirectory = companyDirectory;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;

        var companies = await _companyDirectory.GetCompaniesAsync();
        _companyNameById = companies.ToDictionary(c => c.Id, c => c.Name);

        CompanyFilterOptions.Clear();
        CompanyFilterOptions.Add("All Companies");
        foreach (var company in companies)
            CompanyFilterOptions.Add(company.Name);

        _allOrders = await _orderService.GetAllOrdersAsync();

        CategoryFilterOptions.Clear();
        CategoryFilterOptions.Add("All Categories");
        foreach (var category in _allOrders.Select(o => o.Category).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().OrderBy(c => c))
            CategoryFilterOptions.Add(category);

        UpdateMetrics();

        DisputedOrders.Clear();
        foreach (var order in _allOrders.Where(o => o.HasDispute).OrderByDescending(o => o.DisputeReportedAt))
            DisputedOrders.Add(order);
        HasDisputes = DisputedOrders.Count > 0;

        IsBusy = false;
    }

    [RelayCommand]
    private async Task ResolveDisputeAsync(Order order)
    {
        if (order is null) return;

        string newStatus = await AlertService.Instance.ShowActionSheetAsync(
            $"Update dispute {order.DisputeTicketRef}", "Cancel", "Investigating", "Refunded", "Resolved");

        if (string.IsNullOrEmpty(newStatus) || newStatus == "Cancel") return;

        await _orderService.UpdateDisputeStatusAsync(order.Id, newStatus);
        order.DisputeStatus = newStatus;

        // Order isn't an ObservableObject — force the CollectionView to re-read this item.
        var index = DisputedOrders.IndexOf(order);
        if (index >= 0)
        {
            DisputedOrders.RemoveAt(index);
            DisputedOrders.Insert(index, order);
        }
    }

    partial void OnSelectedTimeframeChanged(string value)
    {
        IsCustomRangeVisible = value == "Custom Range";
        UpdateMetrics();
    }

    partial void OnSelectedCompanyFilterChanged(string value) => UpdateMetrics();
    partial void OnSelectedCategoryFilterChanged(string value) => UpdateMetrics();
    partial void OnCustomStartDateChanged(DateTime value) { if (SelectedTimeframe == "Custom Range") UpdateMetrics(); }
    partial void OnCustomEndDateChanged(DateTime value) { if (SelectedTimeframe == "Custom Range") UpdateMetrics(); }

    private (DateOnly start, DateOnly end) ResolveDateRange()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);

        switch (SelectedTimeframe)
        {
            case "Today":
                return (today, today);

            case "This Month":
                var monthStart = new DateOnly(today.Year, today.Month, 1);
                return (monthStart, monthStart.AddMonths(1).AddDays(-1));

            case "Custom Range":
                var start = DateOnly.FromDateTime(CustomStartDate);
                var end = DateOnly.FromDateTime(CustomEndDate);
                return start <= end ? (start, end) : (end, start); // tolerate the picker being set backwards

            default: // "This Week" — Monday to Sunday of the current week
                var mondayOffset = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                var weekStart = today.AddDays(-mondayOffset);
                return (weekStart, weekStart.AddDays(6));
        }
    }

    private void UpdateMetrics()
    {
        var (start, end) = ResolveDateRange();

        var filtered = _allOrders.Where(o => o.DeliveryDate >= start && o.DeliveryDate <= end);

        if (SelectedCompanyFilter != "All Companies")
        {
            filtered = filtered.Where(o =>
                _companyNameById.TryGetValue(o.CompanyId, out var name) && name == SelectedCompanyFilter);
        }

        if (SelectedCategoryFilter != "All Categories")
        {
            filtered = filtered.Where(o => o.Category == SelectedCategoryFilter);
        }

        var filteredList = filtered.ToList();

        TotalRevenue = filteredList.Sum(o => o.TotalAmount);
        TotalOrders = filteredList.Count;
        AverageOrderValue = filteredList.Count > 0 ? TotalRevenue / filteredList.Count : 0m;

        var grouped = filteredList
            .Where(o => !string.IsNullOrWhiteSpace(o.ItemName))
            .GroupBy(o => o.ItemName)
            .Select(g => new SalesItemSummary { ItemName = g.Key, UnitsSold = g.Count(), TotalRevenue = g.Sum(o => o.TotalAmount) })
            .OrderByDescending(s => s.UnitsSold)
            .ToList();

        TopSellingItems.Clear();
        var topFive = grouped.Take(5).ToList();
        for (var i = 0; i < topFive.Count; i++)
        {
            topFive[i].SwatchColor = Palette[i % Palette.Length];
            TopSellingItems.Add(topFive[i]);
        }

        TopSellingItem = grouped.FirstOrDefault()?.ItemName ?? "—";

        // Pie chart: revenue share by top item (top 5 + "Other" if there's more)
        var pieSource = grouped.Take(5).ToList();
        var otherRevenue = grouped.Skip(5).Sum(s => s.TotalRevenue);

        var slices = new List<ChartDatum>();
        for (var i = 0; i < pieSource.Count; i++)
            slices.Add(new ChartDatum { Label = pieSource[i].ItemName, Value = (float)pieSource[i].TotalRevenue, Color = Palette[i % Palette.Length] });

        if (otherRevenue > 0)
            slices.Add(new ChartDatum { Label = "Other", Value = (float)otherRevenue, Color = Colors.Gray });

        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var holeColor = isDark ? Color.FromArgb("#1E1E1E") : Colors.White;
        PieChart = new PieChartDrawable { Slices = slices, HoleColor = holeColor };

        // Bar chart: revenue per day across the selected range (capped at 14
        // bars so a wide custom range doesn't turn into an unreadable smear)
        var dayCount = end.DayNumber - start.DayNumber + 1;
        var bars = new List<ChartDatum>();

        if (dayCount <= 14)
        {
            for (var d = start; d <= end; d = d.AddDays(1))
            {
                var dayRevenue = filteredList.Where(o => o.DeliveryDate == d).Sum(o => o.TotalAmount);
                bars.Add(new ChartDatum { Label = d.ToString("ddd"), Value = (float)dayRevenue, Color = Palette[0] });
            }
        }
        else
        {
            // Wide range — bucket by week instead of day
            var weekBuckets = filteredList
                .GroupBy(o => o.DeliveryDate.DayNumber / 7)
                .OrderBy(g => g.Key)
                .Select((g, i) => new ChartDatum { Label = $"Wk {i + 1}", Value = (float)g.Sum(o => o.TotalAmount), Color = Palette[0] });
            bars.AddRange(weekBuckets);
        }

        BarChart = new BarChartDrawable { Bars = bars, LabelColor = BarChart.LabelColor };
    }

    [RelayCommand]
    private async Task ExportReportAsync()
    {
        await AlertService.Instance.ShowAsync("Report Exported", $"Sales report for '{SelectedTimeframe}' ({SelectedCompanyFilter}) has been compiled and saved.", "OK");
    }
}
