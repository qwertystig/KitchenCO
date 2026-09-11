using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using YourKitchenCo.Models;
using YourKitchenCo.Services;

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

    [RelayCommand]
    private async Task ShareInvoiceAsync()
    {
        if (Order is null) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("YOUR KITCHEN CO. (PTY) LTD");
        sb.AppendLine("SARS Compliant Tax Invoice");
        sb.AppendLine($"Invoice: {Order.TaxInvoiceNumber}");
        sb.AppendLine("VAT Reg No: 4910298412");
        sb.AppendLine("Corporate Culinary Hub, Sandton, 2196");
        sb.AppendLine();
        sb.AppendLine($"Customer: {CustomerCompanyName}");
        sb.AppendLine($"Delivery: {CustomerLocationName}");
        sb.AppendLine($"Delivery Date: {Order.DeliveryDate:dddd, dd MMM yyyy}");
        sb.AppendLine();
        sb.AppendLine("LINE ITEMS");
        sb.AppendLine($"  {Order.ItemName} — R{MealSubtotal:F2}");
        sb.AppendLine();
        sb.AppendLine($"Meal Subtotal:          R {MealSubtotal:F2}");
        if (Order.SubsidyAmount > 0)
            sb.AppendLine($"Company Subsidy:       -R {Order.SubsidyAmount:F2}");
        if (Order.DiscountAmount > 0)
            sb.AppendLine($"Corporate Discount:    -R {Order.DiscountAmount:F2}");
        sb.AppendLine($"Delivery Fee:           R {Order.DeliveryFee:F2}");
        sb.AppendLine("--------------------------------");
        sb.AppendLine($"Total Paid (ZAR):       R {Order.TotalAmount:F2}");
        sb.AppendLine();
        sb.AppendLine("VAT: Not applicable (zero-rated corporate catering).");

        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Text = sb.ToString(),
            Title = $"Tax Invoice {Order.TaxInvoiceNumber}"
        });
    }
}
