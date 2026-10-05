using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using YourKitchenCo.Services;
using YourKitchenCo.Views;

namespace YourKitchenCo.ViewModel;

/// <summary>
/// Handles the payment step between Cart and order confirmation. This is a
/// demo/mock flow — there's no PCI-compliant backend to actually process a
/// card or a real PayFast merchant integration yet (PayFast requires a live
/// merchant account and server-side ITN/webhook handling to confirm payment
/// safely — that can't live only on the client). Both payment paths here
/// simulate a realistic processing delay and then place the order exactly
/// as if payment had succeeded, so the rest of the app (order history,
/// admin views, reports) has real data to work with while the actual
/// payment gateway integration is still pending.
/// </summary>
public partial class PaymentViewModel : ObservableObject, IQueryAttributable
{
    private readonly ICartService _cartService;
    private readonly ISessionService _session;
    private readonly ICompanyDirectoryService _companyDirectory;
    private readonly IOrderService _orderService;

    [ObservableProperty]
    private decimal _amountDue;

    [ObservableProperty]
    private decimal _deliveryFee;

    [ObservableProperty]
    private string _selectedMethod = "PayFast"; // PayFast is the only method offered in the UI for now

    [ObservableProperty]
    private bool _isCardSelected = false;

    [ObservableProperty]
    private string _cardNumber = string.Empty;

    [ObservableProperty]
    private string _expiryDate = string.Empty;

    [ObservableProperty]
    private string _cvv = string.Empty;

    [ObservableProperty]
    private string _cardholderName = string.Empty;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _processingMessage = "Processing your payment…";

    public PaymentViewModel(ICartService cartService, ISessionService session, ICompanyDirectoryService companyDirectory, IOrderService orderService)
    {
        _cartService = cartService;
        _session = session;
        _companyDirectory = companyDirectory;
        _orderService = orderService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("amountDue", out var amountObj) && decimal.TryParse(amountObj?.ToString(), out var amount))
            AmountDue = amount;

        if (query.TryGetValue("deliveryFee", out var feeObj) && decimal.TryParse(feeObj?.ToString(), out var fee))
            DeliveryFee = fee;
    }

    [RelayCommand]
    private void SelectMethod(string method)
    {
        SelectedMethod = method;
        IsCardSelected = method == "Card";
    }

    [RelayCommand]
    private async Task PayWithCardAsync()
    {
        var digitsOnly = CardNumber.Replace(" ", "");
        if (digitsOnly.Length < 13 || digitsOnly.Length > 19 || !long.TryParse(digitsOnly, out _))
        {
            await AlertService.Instance.ShowAsync("Invalid Card Number", "Please enter a valid card number.", "OK");
            return;
        }

        if (ExpiryDate.Length != 5 || ExpiryDate[2] != '/')
        {
            await AlertService.Instance.ShowAsync("Invalid Expiry Date", "Please enter the expiry date as MM/YY.", "OK");
            return;
        }

        if (Cvv.Length < 3 || Cvv.Length > 4)
        {
            await AlertService.Instance.ShowAsync("Invalid CVV", "Please enter a valid 3 or 4 digit security code.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(CardholderName))
        {
            await AlertService.Instance.ShowAsync("Missing Name", "Please enter the name on the card.", "OK");
            return;
        }

        ProcessingMessage = "Processing your card payment…";
        await CompleteSimulatedPaymentAsync();
    }

    [RelayCommand]
    private async Task PayWithPayFastAsync()
    {
        ProcessingMessage = "Redirecting to PayFast…";
        await CompleteSimulatedPaymentAsync();
    }

    private async Task CompleteSimulatedPaymentAsync()
    {
        IsProcessing = true;

        // Simulated gateway round-trip. Swap this whole block for a real
        // card-tokenization call or a genuine PayFast redirect+ITN webhook
        // once there's a backend able to hold merchant credentials safely.
        await Task.Delay(1800);

        var user = _session.CurrentUser!;
        var company = !string.IsNullOrWhiteSpace(user.CompanyId)
            ? await _companyDirectory.GetCompanyAsync(user.CompanyId)
            : null;

        var createdOrders = await _orderService.PlaceCartOrdersAsync(_cartService.Items, user, company, DeliveryFee);
        _cartService.ClearCart();

        IsProcessing = false;

        var navigationParameter = new Dictionary<string, object> { { "Orders", createdOrders } };
        await Shell.Current.GoToAsync(nameof(OrderConfirmationPage), navigationParameter);
    }
}
