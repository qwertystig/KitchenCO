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
using YourKitchenCo.Services.Export;

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

    // ===================== Production sheet / delivery notes / invoices =====================
    // All three use the orders behind the current Company + Date filters (the
    // same rows the screen is showing), so "today's production sheet" is just
    // the Date filter set to today.

    private string FilterLine() => $"Company: {SelectedCompanyFilter}    Date: {SelectedDateFilter}";
    private string FileStamp() => (SelectedDateFilter == "All Dates" ? "all-dates" : SelectedDateFilter.Replace(", ", "_").Replace(" ", "")) + "_" + (SelectedCompanyFilter == "All Companies" ? "all" : SelectedCompanyFilter.Replace(" ", ""));

    private async Task<bool> EnsureRowsAsync()
    {
        if (Orders.Count > 0) return true;
        await AlertService.Instance.ShowAsync("Nothing to Export", "There are no orders matching the current filters.", "OK");
        return false;
    }

    /// <summary>
    /// Production sheet = what the kitchen preps from: (1) totals per dish,
    /// (2) every order line per delivery date → company/location → person
    /// with quantity, notes and allergy flags — i.e. the Order Report and the
    /// Delivery Notes together on one document, in the order it's worked.
    /// </summary>
    [RelayCommand]
    private async Task DownloadProductionSheetPdfAsync()
    {
        if (!await EnsureRowsAsync()) return;

        var pdf = new SimplePdfWriter();
        pdf.Heading("YOUR KITCHEN CO. - PRODUCTION SHEET", 16, 2);
        pdf.Text(FilterLine(), 10);
        pdf.Text($"Generated {DateTime.Now:dddd, dd MMM yyyy HH:mm}", 9, 10);

        pdf.SubHeading("1. PREP TOTALS (make this many of each)", 12, 4);
        foreach (var item in PrepSummary)
            pdf.Mono($"  {item.TotalQuantity,3} x  {item.DishName}", 10);
        pdf.Mono($"  Total meals: {PrepSummary.Sum(p => p.TotalQuantity)}   Distinct dishes: {PrepSummary.Count}", 9, 12);

        pdf.SubHeading("2. DELIVERY NOTES (labelling - one line per order)", 12, 4);
        WriteDeliveryNotes(pdf);

        await ExportService.SharePdfAsync($"Production_Sheet_{FileStamp()}", pdf, "Production Sheet");
    }

    [RelayCommand]
    private async Task DownloadProductionSheetExcelAsync()
    {
        if (!await EnsureRowsAsync()) return;

        var xlsx = new SimpleXlsxWriter();

        var totals = xlsx.AddSheet("Prep Totals").Widths(36, 10);
        totals.Header("Dish", "Quantity");
        foreach (var item in PrepSummary) totals.Row(item.DishName, item.TotalQuantity);
        totals.Blank();
        totals.Row("Total meals", PrepSummary.Sum(p => p.TotalQuantity));

        var orders = xlsx.AddSheet("Order Report").Widths(16, 22, 24, 30, 36, 8, 12, 18, 16, 14, 30, 30);
        orders.Header("Delivery Date", "Company", "Location", "Customer", "Dish", "Qty", "Order #", "Ordered", "Floor / Desk", "Status", "Notes", "Allergy");
        foreach (var r in Orders.OrderBy(r => r.Order.DeliveryDate).ThenBy(r => r.CompanyName).ThenBy(r => r.Order.CustomerName))
        {
            var (dish, qty) = r.Order.ParseItemNameAndQuantity();
            orders.Row(r.Order.DeliveryDate.ToString("ddd dd MMM yyyy"), r.CompanyName, r.LocationName, r.Order.CustomerName, dish, qty, r.Order.OrderNumber,
                       r.Order.OrderDate.ToString("dd MMM yyyy HH:mm"), r.Order.DeliveryFloor, r.Order.Status, r.Order.SummaryText, r.Order.AllergyNotes);
        }

        // Labels sheet: one row per unit so it can be mail-merged straight onto label stationery.
        var labels = xlsx.AddSheet("Labels").Widths(30, 36, 22, 24, 16, 30);
        labels.Header("Customer", "Dish", "Company", "Location", "Delivery", "Allergy");
        foreach (var r in Orders.OrderBy(r => r.Order.DeliveryDate).ThenBy(r => r.CompanyName).ThenBy(r => r.Order.CustomerName))
        {
            var (dish, qty) = r.Order.ParseItemNameAndQuantity();
            for (var i = 0; i < qty; i++)
                labels.Row(r.Order.CustomerName, dish, r.CompanyName, r.LocationName, r.Order.DeliveryDate.ToString("ddd dd MMM"), r.Order.AllergyNotes);
        }

        await ExportService.ShareXlsxAsync($"Production_Sheet_{FileStamp()}", xlsx, "Production Sheet");
    }

    [RelayCommand]
    private async Task DownloadDeliveryNotesPdfAsync()
    {
        if (!await EnsureRowsAsync()) return;

        var pdf = new SimplePdfWriter();
        pdf.Heading("YOUR KITCHEN CO. - DELIVERY NOTES", 16, 2);
        pdf.Text(FilterLine(), 10);
        pdf.Text($"Generated {DateTime.Now:dddd, dd MMM yyyy HH:mm}", 9, 10);
        WriteDeliveryNotes(pdf);
        await ExportService.SharePdfAsync($"Delivery_Notes_{FileStamp()}", pdf, "Delivery Notes");
    }

    private void WriteDeliveryNotes(SimplePdfWriter pdf)
    {
        var labelCount = 0;
        foreach (var byDate in Orders.GroupBy(r => r.Order.DeliveryDate).OrderBy(g => g.Key))
        {
            pdf.SubHeading($"DELIVERY: {byDate.Key:dddd, dd MMMM yyyy}", 11, 2);
            foreach (var byCompany in byDate.GroupBy(r => r.CompanyName + " | " + r.LocationName).OrderBy(g => g.Key))
            {
                var first = byCompany.First();
                pdf.Text($"{first.CompanyName} - {first.LocationName}", 10, 2);
                foreach (var byPerson in byCompany.GroupBy(r => r.Order.CustomerName).OrderBy(g => g.Key))
                {
                    var floor = byPerson.Select(r => r.Order.DeliveryFloor).FirstOrDefault(f => !string.IsNullOrWhiteSpace(f));
                    pdf.Mono($"  {byPerson.Key}{(string.IsNullOrWhiteSpace(floor) ? string.Empty : $"  ({floor})")}", 10);
                    foreach (var r in byPerson.OrderBy(r => r.Order.ItemName))
                    {
                        var (dish, qty) = r.Order.ParseItemNameAndQuantity();
                        labelCount += qty;
                        pdf.Mono($"     [ ] {qty} x {dish}   #{r.Order.OrderNumber}", 9);
                        if (r.Order.HasAllergyNotes) pdf.Mono($"         !! ALLERGY: {r.Order.AllergyNotes}", 9);
                        if (!string.IsNullOrWhiteSpace(r.Order.SummaryText)) pdf.Mono($"         Note: {r.Order.SummaryText}", 9);
                    }
                }
                pdf.Blank(4);
            }
        }
        pdf.Rule(64);
        pdf.Mono($"Labels: {labelCount}   People: {Orders.Select(r => r.Order.CustomerName).Distinct().Count()}   Order lines: {Orders.Count}", 9);
    }

    /// <summary>
    /// Every invoice for the filtered orders, one per page, built by the same
    /// InvoiceDocument the customer's invoice screen uses — so the printed
    /// copy matches what the client sees on their phone.
    /// </summary>
    [RelayCommand]
    private async Task DownloadInvoicesPdfAsync()
    {
        if (!await EnsureRowsAsync()) return;

        var pdf = new SimplePdfWriter();
        var first = true;
        foreach (var r in Orders.OrderBy(r => r.Order.DeliveryDate).ThenBy(r => r.CompanyName).ThenBy(r => r.Order.CustomerName))
        {
            var doc = await InvoiceDocument.LoadAsync(r.Order, _companyDirectory);
            doc.WriteTo(pdf, pageBreakBefore: !first);
            first = false;
        }

        await ExportService.SharePdfAsync($"Invoices_{FileStamp()}", pdf, "Invoices");
    }
}
