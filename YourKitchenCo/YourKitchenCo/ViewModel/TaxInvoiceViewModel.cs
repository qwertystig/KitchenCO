using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using YourKitchenCo.Models;
using YourKitchenCo.Services;
using YourKitchenCo.Services.Export;

namespace YourKitchenCo.ViewModel;

public partial class TaxInvoiceViewModel : ObservableObject, IQueryAttributable
{
    private readonly ICompanyDirectoryService _companyDirectory;

    [ObservableProperty]
    private Order? _order;

    [ObservableProperty]
    private string _customerCompanyName = string.Empty;

    [ObservableProperty]
    private string _customerLocationName = string.Empty;

    // Subtotal isn't stored directly — Order only keeps the final
    // TotalAmount, so this backs it out from the known subsidy/discount/
    // delivery-fee components rather than adding a redundant stored field
    // that could drift out of sync with TotalAmount.
    public decimal MealSubtotal => (Order?.TotalAmount ?? 0) - (Order?.DeliveryFee ?? 0) + (Order?.SubsidyAmount ?? 0) + (Order?.DiscountAmount ?? 0);

    public TaxInvoiceViewModel(ICompanyDirectoryService companyDirectory)
    {
        _companyDirectory = companyDirectory;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Order", out var orderObj) && orderObj is Order order)
        {
            Order = order;

            var company = !string.IsNullOrWhiteSpace(order.CompanyId) ? await _companyDirectory.GetCompanyAsync(order.CompanyId) : null;
            var location = !string.IsNullOrWhiteSpace(order.LocationId) ? await _companyDirectory.GetLocationAsync(order.LocationId) : null;

            CustomerCompanyName = company?.Name ?? "Individual Customer";
            CustomerLocationName = location?.Address ?? location?.Name ?? "—";

            OnPropertyChanged(nameof(MealSubtotal));
        }
    }

    private async Task<InvoiceDocument?> BuildDocumentAsync()
    {
        if (Order is null) return null;
        // Single source of truth shared with the admin "invoices for the day"
        // batch, so the customer's copy and the printed copy match exactly.
        return await InvoiceDocument.LoadAsync(Order, _companyDirectory);
    }

    [RelayCommand]
    private async Task ShareInvoiceAsync()
    {
        var doc = await BuildDocumentAsync();
        if (doc is null) return;

        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Text = doc.ToText(),
            Title = $"Invoice {Order!.TaxInvoiceNumber}"
        });
    }

    /// <summary>Downloads this invoice as a PDF (via the system share sheet: Save to Files / Drive / email / print).</summary>
    [RelayCommand]
    private async Task DownloadInvoicePdfAsync()
    {
        var doc = await BuildDocumentAsync();
        if (doc is null) return;

        var pdf = new SimplePdfWriter();
        doc.WriteTo(pdf);
        await ExportService.SharePdfAsync($"Invoice_{Order!.TaxInvoiceNumber}", pdf, $"Invoice {Order.TaxInvoiceNumber}");
    }
}
