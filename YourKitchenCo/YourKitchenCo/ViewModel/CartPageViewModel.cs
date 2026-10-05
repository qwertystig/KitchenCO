using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using YourKitchenCo.Models;
using YourKitchenCo.Services;

namespace YourKitchenCo.ViewModel;

public partial class CartPageViewModel : ObservableObject
{
    private readonly ICartService _cartService;
    private readonly IOrderSchedulingService _schedulingService;
    private readonly ISessionService _session;
    private readonly ICompanyDirectoryService _companyDirectory;

    public ObservableCollection<CartItem> CartItems => _cartService.Items;

    [ObservableProperty]
    private decimal _cartTotal;

    [ObservableProperty]
    private decimal _companySubsidyTotal;

    [ObservableProperty]
    private decimal _amountDue;

    [ObservableProperty]
    private decimal _deliveryFee;

    [ObservableProperty]
    private string _deliveryFeeLabel = string.Empty;

    [ObservableProperty]
    private bool _isOutsideDeliveryRange;

    [ObservableProperty]
    private string _subsidyLabel = string.Empty;

    [ObservableProperty]
    private bool _isCartEmpty;

    [ObservableProperty]
    private bool _hasItems;

    [ObservableProperty]
    private bool _isOrderingOpen = true;

    public CartPageViewModel(ICartService cartService, IOrderSchedulingService schedulingService, ISessionService session, ICompanyDirectoryService companyDirectory)
    {
        _cartService = cartService;
        _schedulingService = schedulingService;
        _session = session;
        _companyDirectory = companyDirectory;
        CartItems.CollectionChanged += OnCartItemsChanged;
        UpdateCartState();
    }

    public void Refresh()
    {
        UpdateCartState();
        OnPropertyChanged(nameof(CartItems));
    }

    [RelayCommand]
    private void RemoveItem(CartItem item)
    {
        if (item != null)
        {
            _cartService.RemoveItem(item);
            UpdateCartState();
        }
    }

    [RelayCommand]
    private void IncreaseQuantity(CartItem item)
    {
        if (item == null) return;
        item.Quantity++;
        UpdateCartState();
    }

    [RelayCommand]
    private void DecreaseQuantity(CartItem item)
    {
        if (item == null) return;

        if (item.Quantity > 1)
        {
            item.Quantity--;
            UpdateCartState();
        }
        else
        {
            RemoveItem(item);
        }
    }

    [RelayCommand]
    private async Task CheckoutAsync()
    {
        if (CartItems.Count == 0) return;

        if (!_schedulingService.IsOrderingOpen())
        {
            await AlertService.Instance.ShowAsync("Ordering closed", "Ordering is temporarily closed — please check back shortly.", "OK");
            return;
        }

        // Defensive re-check: an item's delivery date could have fallen out of
        // the allowed window if the cart was left open across a cutoff.
        var invalidItems = CartItems
            .Where(item =>
            {
                var window = item.MenuType == MenuType.Cycle ? 5 : 10;
                return !_schedulingService.IsValidDeliveryDate(item.DeliveryDate, window);
            })
            .ToList();

        if (invalidItems.Count > 0)
        {
            var names = string.Join(", ", invalidItems.Select(i => i.Product.Name));
            await AlertService.Instance.ShowAsync(
                "Some items need a new date",
                $"These items are past their order cutoff and need a new delivery date: {names}",
                "OK");
            return;
        }

        if (!_session.IsLoggedIn)
        {
            await AlertService.Instance.ShowAsync("Please log in", "You need to be logged in to place an order.", "OK");
            return;
        }

        if (IsOutsideDeliveryRange)
        {
            await AlertService.Instance.ShowAsync(
                "Outside Delivery Range",
                "Your delivery location is beyond our 50km delivery range. Please contact us directly to arrange this order.",
                "OK");
            return;
        }

        // Actual order creation now happens after payment — see PaymentPage/PaymentViewModel.
        await Shell.Current.GoToAsync($"{nameof(Views.PaymentPage)}?amountDue={AmountDue}&deliveryFee={DeliveryFee}");
    }

    private void OnCartItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateCartState();

    private void UpdateCartState()
    {
        CartTotal = _cartService.GetTotal();
        IsCartEmpty = CartItems.Count == 0;
        HasItems = CartItems.Count > 0;
        IsOrderingOpen = _schedulingService.IsOrderingOpen();

        _ = RecalculateSubsidyAsync();
    }

    private async Task RecalculateSubsidyAsync()
    {
        var user = _session.CurrentUser;
        var companyId = user?.CompanyId;

        // Delivery fee: the company's own delivery setting first (free / flat
        // fee, set by admin), falling back to the distance tier for the
        // customer's location.
        var companyForDelivery = string.IsNullOrWhiteSpace(companyId) ? null : await _companyDirectory.GetCompanyAsync(companyId);
        await RecalculateDeliveryFeeAsync(user?.LocationId, companyForDelivery);

        if (string.IsNullOrWhiteSpace(companyId))
        {
            CompanySubsidyTotal = 0;
            AmountDue = CartTotal + DeliveryFee;
            SubsidyLabel = string.Empty;
            return;
        }

        var company = await _companyDirectory.GetCompanyAsync(companyId);
        if (company is null || company.MealSubsidyAmount <= 0)
        {
            CompanySubsidyTotal = 0;
            AmountDue = CartTotal + DeliveryFee;
            SubsidyLabel = string.Empty;
            return;
        }

        // The subsidy covers up to its amount on each individual meal — it
        // doesn't carry over as one lump discount across the whole basket,
        // and it never pushes an item's own contribution below zero.
        var subsidy = CartItems.Sum(item => Math.Min(company.MealSubsidyAmount, item.FinalPrice) * item.Quantity);

        CompanySubsidyTotal = subsidy;
        AmountDue = Math.Max(0, CartTotal - subsidy) + DeliveryFee;
        SubsidyLabel = $"{company.Name} covers R{company.MealSubsidyAmount:F2} per meal (incl. VAT)";
    }

    private async Task RecalculateDeliveryFeeAsync(string? locationId, Company? company)
    {
        if (CartItems.Count == 0)
        {
            DeliveryFee = 0;
            DeliveryFeeLabel = string.Empty;
            IsOutsideDeliveryRange = false;
            return;
        }

        // Company-level override (Admin > Companies > Delivery) wins over the
        // distance tier — free or a flat amount applies regardless of location.
        if (company?.DeliveryFeeMode == DeliveryFeeMode.Free)
        {
            DeliveryFee = 0;
            IsOutsideDeliveryRange = false;
            DeliveryFeeLabel = $"Delivery: Free (covered by {company.Name})";
            return;
        }
        if (company?.DeliveryFeeMode == DeliveryFeeMode.Flat)
        {
            DeliveryFee = Math.Max(0, company.FlatDeliveryFee);
            IsOutsideDeliveryRange = false;
            DeliveryFeeLabel = DeliveryFee == 0
                ? $"Delivery: Free (covered by {company.Name})"
                : $"Delivery (flat rate for {company.Name}): R{DeliveryFee:F2}";
            return;
        }

        if (string.IsNullOrWhiteSpace(locationId))
        {
            DeliveryFee = 0;
            DeliveryFeeLabel = string.Empty;
            IsOutsideDeliveryRange = false;
            return;
        }

        var location = await _companyDirectory.GetLocationAsync(locationId);
        if (location is null)
        {
            DeliveryFee = 0;
            DeliveryFeeLabel = string.Empty;
            IsOutsideDeliveryRange = false;
            return;
        }

        var fee = DeliveryFeeCalculator.GetFee(location.DistanceKm);
        if (fee is null)
        {
            DeliveryFee = 0;
            IsOutsideDeliveryRange = true;
            DeliveryFeeLabel = $"{location.DistanceKm:F1}km is outside our delivery range — please contact us";
            return;
        }

        IsOutsideDeliveryRange = false;
        DeliveryFee = fee.Value;
        DeliveryFeeLabel = $"Delivery ({DeliveryFeeCalculator.GetTierLabel(location.DistanceKm)}): R{fee.Value:F2}";
    }
}