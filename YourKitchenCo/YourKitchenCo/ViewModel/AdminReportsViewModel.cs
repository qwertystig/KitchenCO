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
using YourKitchenCo.Services.Export;

namespace YourKitchenCo.ViewModel;

public class SalesItemSummary
{
    public string ItemName { get; set; } = string.Empty;
    public int UnitsSold { get; set; }
    public decimal TotalRevenue { get; set; }
    public Color SwatchColor { get; set; } = Colors.Gray;
}

/// <summary>Generic "name · count · amount" row used by the extra reports.</summary>
public class NameValueRow
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
    public string Detail { get; set; } = string.Empty;
}


public partial class AdminReportsViewModel : ObservableObject
{
    /// <summary>The orders behind the current filters — shared by the metrics, the Order Report and the Delivery Notes.</summary>
    private List<Order> _filteredOrders = new();


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
    private readonly IProductService _productService;

    private List<Order> _allOrders = new();
    private List<string> _menuItemNames = new();
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

    public AdminReportsViewModel(IOrderService orderService, ICompanyDirectoryService companyDirectory, IProductService productService)
    {
        _orderService = orderService;
        _companyDirectory = companyDirectory;
        _productService = productService;
        _ = LoadAsync();
    }

    // ===== Extra report collections (all follow the same filters) =====
    public ObservableCollection<SalesItemSummary> TopPerformers { get; } = new();
    public ObservableCollection<SalesItemSummary> BottomPerformers { get; } = new();
    public ObservableCollection<NameValueRow> RevenueByCompany { get; } = new();
    public ObservableCollection<NameValueRow> RevenueByCategory { get; } = new();
    public ObservableCollection<NameValueRow> OrdersByDeliveryDay { get; } = new();
    public ObservableCollection<NameValueRow> TopCustomers { get; } = new();
    public ObservableCollection<NameValueRow> CompanyBenefitCosts { get; } = new();

    [ObservableProperty]
    private bool _isExtraReportsExpanded;

    [RelayCommand]
    private void ToggleExtraReports() => IsExtraReportsExpanded = !IsExtraReportsExpanded;

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

        // Menu names so "worst performers" can include dishes that never sold at all.
        try
        {
            var products = await _productService.GetProductsAsync();
            _menuItemNames = products.Select(p => p.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();
        }
        catch
        {
            _menuItemNames = new List<string>();
        }

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
        _filteredOrders = filteredList;

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

        // ----- Top 10 / bottom 10 -----
        // Units are counted per dish name (quantity parsed out of "2x Dish"),
        // not per order line, so a 3-portion order counts as 3.
        var unitsByDish = filteredList
            .Select(o => (parsed: o.ParseItemNameAndQuantity(), o.TotalAmount))
            .GroupBy(x => x.parsed.DishName)
            .ToDictionary(g => g.Key, g => (Units: g.Sum(x => x.parsed.Quantity), Revenue: g.Sum(x => x.TotalAmount)));

        TopPerformers.Clear();
        foreach (var kv in unitsByDish.OrderByDescending(kv => kv.Value.Units).ThenByDescending(kv => kv.Value.Revenue).Take(10))
            TopPerformers.Add(new SalesItemSummary { ItemName = kv.Key, UnitsSold = kv.Value.Units, TotalRevenue = kv.Value.Revenue });

        // Worst: every menu dish with zero sales first, then the lowest sellers.
        var bottom = _menuItemNames
            .Where(n => !unitsByDish.ContainsKey(n))
            .Select(n => new SalesItemSummary { ItemName = n, UnitsSold = 0, TotalRevenue = 0 })
            .Concat(unitsByDish.OrderBy(kv => kv.Value.Units).ThenBy(kv => kv.Value.Revenue)
                .Select(kv => new SalesItemSummary { ItemName = kv.Key, UnitsSold = kv.Value.Units, TotalRevenue = kv.Value.Revenue }))
            .Take(10);
        BottomPerformers.Clear();
        foreach (var b in bottom) BottomPerformers.Add(b);

        // ----- Revenue by company -----
        RevenueByCompany.Clear();
        foreach (var g in filteredList.GroupBy(o => _companyNameById.TryGetValue(o.CompanyId, out var n) ? n : "Unassigned").OrderByDescending(g => g.Sum(o => o.TotalAmount)))
            RevenueByCompany.Add(new NameValueRow { Name = g.Key, Count = g.Count(), Amount = g.Sum(o => o.TotalAmount) });

        // ----- Revenue by category -----
        RevenueByCategory.Clear();
        foreach (var g in filteredList.GroupBy(o => string.IsNullOrWhiteSpace(o.Category) ? "Uncategorised" : o.Category).OrderByDescending(g => g.Sum(o => o.TotalAmount)))
            RevenueByCategory.Add(new NameValueRow { Name = g.Key, Count = g.Count(), Amount = g.Sum(o => o.TotalAmount) });

        // ----- Orders per delivery day (demand curve) -----
        OrdersByDeliveryDay.Clear();
        foreach (var g in filteredList.GroupBy(o => o.DeliveryDate).OrderBy(g => g.Key))
            OrdersByDeliveryDay.Add(new NameValueRow { Name = g.Key.ToString("ddd dd MMM"), Count = g.Sum(o => o.ParseItemNameAndQuantity().Quantity), Amount = g.Sum(o => o.TotalAmount), Detail = g.Key.ToString("dddd") });

        // ----- Top customers by spend -----
        TopCustomers.Clear();
        foreach (var g in filteredList.GroupBy(o => string.IsNullOrWhiteSpace(o.CustomerName) ? "Unknown" : o.CustomerName).OrderByDescending(g => g.Sum(o => o.TotalAmount)).Take(10))
            TopCustomers.Add(new NameValueRow { Name = g.Key, Count = g.Count(), Amount = g.Sum(o => o.TotalAmount), Detail = _companyNameById.TryGetValue(g.First().CompanyId, out var cn) ? cn : string.Empty });

        // ----- What each company's subsidy/discount is costing -----
        CompanyBenefitCosts.Clear();
        foreach (var g in filteredList.GroupBy(o => _companyNameById.TryGetValue(o.CompanyId, out var n) ? n : "Unassigned").OrderByDescending(g => g.Sum(o => o.SubsidyAmount + o.DiscountAmount)))
            CompanyBenefitCosts.Add(new NameValueRow { Name = g.Key, Count = g.Count(), Amount = g.Sum(o => o.SubsidyAmount + o.DiscountAmount), Detail = $"subsidy R{g.Sum(o => o.SubsidyAmount):F2} · discount R{g.Sum(o => o.DiscountAmount):F2}" });

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

    private string FilterDescription()
    {
        var (start, end) = ResolveDateRange();
        var range = start == end ? start.ToString("ddd dd MMM yyyy") : $"{start:dd MMM yyyy} – {end:dd MMM yyyy}";
        return $"{SelectedTimeframe} ({range}) · {SelectedCompanyFilter}";
    }



    // ===================== Excel downloads =====================
    // One sheet per report (or every sheet in one workbook with "Export All").
    // Each goes through the system share sheet, like the invoice.

    private string FileStamp()
    {
        var (start, end) = ResolveDateRange();
        return $"{start:yyyy-MM-dd}_to_{end:yyyy-MM-dd}";
    }

    private void FillSheet(SimpleXlsxWriter xlsx, string report)
    {
        switch (report)
        {
            case "Top Performers":
                var t = xlsx.AddSheet("Top 10 Performers").Widths(40, 12, 14);
                t.Header("Dish", "Units Sold", "Revenue (R)");
                foreach (var r in TopPerformers) t.Row(r.ItemName, r.UnitsSold, r.TotalRevenue);
                break;
            case "Bottom Performers":
                var b = xlsx.AddSheet("Bottom 10 Performers").Widths(40, 12, 14);
                b.Header("Dish", "Units Sold", "Revenue (R)");
                foreach (var r in BottomPerformers) b.Row(r.ItemName, r.UnitsSold, r.TotalRevenue);
                break;
            case "Revenue by Company":
                var c = xlsx.AddSheet("Revenue by Company").Widths(32, 12, 14);
                c.Header("Company", "Orders", "Revenue (R)");
                foreach (var r in RevenueByCompany) c.Row(r.Name, r.Count, r.Amount);
                c.Blank(); c.Row("Total", RevenueByCompany.Sum(r => r.Count), RevenueByCompany.Sum(r => r.Amount));
                break;
            case "Revenue by Category":
                var k = xlsx.AddSheet("Revenue by Category").Widths(32, 12, 14);
                k.Header("Category", "Orders", "Revenue (R)");
                foreach (var r in RevenueByCategory) k.Row(r.Name, r.Count, r.Amount);
                break;
            case "Orders by Delivery Day":
                var d = xlsx.AddSheet("Orders by Delivery Day").Widths(16, 14, 10, 14);
                d.Header("Delivery Date", "Weekday", "Meals", "Revenue (R)");
                foreach (var r in OrdersByDeliveryDay) d.Row(r.Name, r.Detail, r.Count, r.Amount);
                break;
            case "Top Customers":
                var u = xlsx.AddSheet("Top Customers").Widths(30, 28, 10, 14);
                u.Header("Customer", "Company", "Orders", "Spend (R)");
                foreach (var r in TopCustomers) u.Row(r.Name, r.Detail, r.Count, r.Amount);
                break;
            case "Company Benefit Costs":
                var s = xlsx.AddSheet("Subsidy & Discount Costs").Widths(30, 10, 16, 40);
                s.Header("Company", "Orders", "Total Cost (R)", "Breakdown");
                foreach (var r in CompanyBenefitCosts) s.Row(r.Name, r.Count, r.Amount, r.Detail);
                break;
            case "All Orders":
                var o = xlsx.AddSheet("All Orders").Widths(14, 18, 24, 28, 36, 8, 22, 14, 14, 12, 12, 12);
                o.Header("Order #", "Ordered", "Customer", "Company", "Dish", "Qty", "Category", "Delivery", "Status", "Total (R)", "Subsidy (R)", "Discount (R)");
                foreach (var x in _filteredOrders.OrderBy(x => x.OrderDate))
                {
                    var (dish, qty) = x.ParseItemNameAndQuantity();
                    o.Row(x.OrderNumber, x.OrderDate.ToString("dd MMM yyyy HH:mm"), x.CustomerName,
                          _companyNameById.TryGetValue(x.CompanyId, out var cn) ? cn : "", dish, qty, x.Category,
                          x.DeliveryDate.ToString("ddd dd MMM yyyy"), x.Status, x.TotalAmount, x.SubsidyAmount, x.DiscountAmount);
                }
                break;
            case "Disputes":
                var p = xlsx.AddSheet("Disputes").Widths(14, 14, 24, 36, 40, 16, 18);
                p.Header("Ticket", "Order #", "Customer", "Item", "Reason", "Status", "Reported");
                foreach (var x in DisputedOrders) p.Row(x.DisputeTicketRef, x.OrderNumber, x.CustomerName, x.ItemName, x.DisputeReason, x.DisputeStatus, x.DisputeReportedAt?.ToString("dd MMM yyyy") ?? "");
                break;
        }
    }

    private static readonly string[] AllReports =
    {
        "Top Performers", "Bottom Performers", "Revenue by Company", "Revenue by Category",
        "Orders by Delivery Day", "Top Customers", "Company Benefit Costs", "All Orders", "Disputes"
    };

    [RelayCommand]
    private async Task ExportReportExcelAsync(string report)
    {
        if (string.IsNullOrWhiteSpace(report)) return;
        var xlsx = new SimpleXlsxWriter();
        var summary = xlsx.AddSheet("Summary").Widths(28, 40);
        summary.Header("Report", report);
        summary.Row("Filters", FilterDescription());
        summary.Row("Generated", DateTime.Now.ToString("dd MMM yyyy HH:mm"));
        summary.Row("Orders in range", TotalOrders);
        summary.Row("Revenue in range (R)", TotalRevenue);
        FillSheet(xlsx, report);
        await ExportService.ShareXlsxAsync($"{report.Replace(' ', '_')}_{FileStamp()}", xlsx, report);
    }

    [RelayCommand]
    private async Task ExportAllReportsExcelAsync()
    {
        var xlsx = new SimpleXlsxWriter();
        var summary = xlsx.AddSheet("Summary").Widths(28, 40);
        summary.Header("Your Kitchen Co. — Reports", "");
        summary.Row("Filters", FilterDescription());
        summary.Row("Generated", DateTime.Now.ToString("dd MMM yyyy HH:mm"));
        summary.Row("Orders in range", TotalOrders);
        summary.Row("Revenue in range (R)", TotalRevenue);
        summary.Row("Average order value (R)", AverageOrderValue);
        summary.Row("Top seller", TopSellingItem);
        foreach (var r in AllReports) FillSheet(xlsx, r);
        await ExportService.ShareXlsxAsync($"All_Reports_{FileStamp()}", xlsx, "All Reports");
    }
}
