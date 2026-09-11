using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using YourKitchenCo.Models;
using YourKitchenCo.Services;
using YourKitchenCo.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace YourKitchenCo.ViewModel;

[QueryProperty(nameof(SelectedProduct), "SelectedProduct")]
public partial class ProductDetailViewModel : ObservableObject
{
    private readonly ICartService _cartService;
    private readonly IOrderSchedulingService _schedulingService;
    private readonly ICycleMenuService _cycleMenuService;
    private readonly ISessionService _session;
    private bool _isUpdatingSelection; // FIXED: Recursion guard flag to prevent infinite calculation loops

    [ObservableProperty]
    private Product selectedProduct;

    [ObservableProperty]
    private decimal totalPrice;

    [ObservableProperty]
    private string specialRequests;

    [ObservableProperty]
    private string allergyNotes;

    // Delivery-date picker state. Window is 5 weekdays for the cycle menu
    // (order across the coming week) and 10 for the static menu (2 weeks).
    [ObservableProperty]
    private DeliveryDateOption? selectedDeliveryDate;

    [ObservableProperty]
    private bool isOrderingOpen = true;

    public ObservableCollection<DeliveryDateOption> AvailableDeliveryDates { get; } = new();

    public ObservableCollection<CustomizationGroup> CustomizationGroups { get; set; } = new();

    public ProductDetailViewModel(ICartService cartService, IOrderSchedulingService schedulingService, ICycleMenuService cycleMenuService, ISessionService session)
    {
        _cartService = cartService;
        _schedulingService = schedulingService;
        _cycleMenuService = cycleMenuService;
        _session = session;
    }

    partial void OnSelectedProductChanged(Product value)
    {
        if (value != null)
        {
            TotalPrice = value.BasePrice;
            LoadCustomizations(value);
            _ = LoadDeliveryDatesAsync(value);
        }
    }

    private async Task LoadDeliveryDatesAsync(Product product)
    {
        // This only drives an informational banner now — it does NOT block
        // browsing or adding to the basket. GetOrderableDeliveryDates already
        // rolls forward past the weekend correctly on its own (an order
        // "placed" on Sunday just becomes a Monday order under the hood), so
        // there's no reason building a basket needs to wait for Monday. The
        // actual "no orders accepted on weekends" rule is enforced once, at
        // checkout, in CartPageViewModel.
        IsOrderingOpen = _schedulingService.IsOrderingOpen();

        AvailableDeliveryDates.Clear();
        SelectedDeliveryDate = null;

        // Cycle-menu items only exist on the one day they were served for —
        // no picker needed, just lock straight to it. No CycleMenu preview is
        // set here since it would show that day's Main Meal text regardless
        // of which category this specific item actually is — the dish itself
        // is already shown prominently at the top of the page.
        if (product.LockedDeliveryDate.HasValue)
        {
            var lockedOption = new DeliveryDateOption { Date = product.LockedDeliveryDate.Value };

            AvailableDeliveryDates.Add(lockedOption);
            SelectedDeliveryDate = lockedOption;
            return;
        }

        var window = product.MenuType == MenuType.Cycle ? 5 : 10;
        var dates = _schedulingService.GetOrderableDeliveryDates(window);

        foreach (var date in dates)
        {
            var option = new DeliveryDateOption { Date = date };

            if (product.MenuType == MenuType.Cycle)
                option.CycleMenu = await _cycleMenuService.GetMenuForDateAsync(date);

            AvailableDeliveryDates.Add(option);
        }

        SelectedDeliveryDate = AvailableDeliveryDates.FirstOrDefault(d => d.Date == _session.SelectedOrderingDate)
                               ?? AvailableDeliveryDates.FirstOrDefault();
    }

    private void LoadCustomizations(Product product)
    {
        // Clean old subscriptions first to strictly avoid memory leaks
        foreach (var group in CustomizationGroups)
        {
            if (group.Options != null)
            {
                foreach (var opt in group.Options)
                {
                    opt.PropertyChanged -= OnOptionSelectionChanged;
                }
            }
        }

        CustomizationGroups.Clear();
        SpecialRequests = string.Empty; // FIXED: Reset the editor text state for a fresh user visit
        AllergyNotes = string.Empty;

        if (product?.CustomizationGroups == null) return;

        foreach (var group in product.CustomizationGroups)
        {
            if (group.Options == null) continue;

            foreach (var option in group.Options)
            {
                // FIXED: Reset active state flags so the global service template catalog stays clean
                option.IsSelected = false;
                option.PropertyChanged += OnOptionSelectionChanged;
            }
            CustomizationGroups.Add(group);
        }
        RecalculateTotal();
    }

    private void OnOptionSelectionChanged(object sender, PropertyChangedEventArgs e)
    {
        // FIXED: Drop out early if this change event was fired internally by our own radio button clearing loop
        if (_isUpdatingSelection) return;

        // Listen exclusively to changes on the IsSelected flag
        if (e.PropertyName == nameof(Option.IsSelected))
        {
            if (sender is Option changedOption && changedOption.IsSelected)
            {
                _isUpdatingSelection = true;
                try
                {
                    // Manage Single-Selection (Radio group rules) purely in C# logic to keep things solid
                    var group = CustomizationGroups.FirstOrDefault(g => g.Options.Contains(changedOption));
                    if (group != null && !group.IsMultiSelect)
                    {
                        foreach (var option in group.Options)
                        {
                            if (option != changedOption && option.IsSelected)
                            {
                                option.IsSelected = false; // Triggers PropertyChanged, but caught gracefully by our guard flag
                            }
                        }
                    }
                }
                finally
                {
                    _isUpdatingSelection = false;
                }
            }

            // Instantly update running financials
            RecalculateTotal();
        }
    }

    private void RecalculateTotal()
    {
        decimal newTotal = SelectedProduct?.BasePrice ?? 0;

        foreach (var group in CustomizationGroups)
        {
            if (group.Options == null) continue;

            foreach (var option in group.Options)
            {
                if (option.IsSelected)
                {
                    newTotal += option.AdditionalPrice;
                }
            }
        }

        TotalPrice = newTotal;
    }

    [RelayCommand]
    private async Task AddToCart()
    {
        if (SelectedProduct == null) return;

        if (SelectedDeliveryDate == null)
        {
            await AlertService.Instance.ShowAsync("Pick a delivery day", "Please choose which day you'd like this delivered.", "OK");
            return;
        }

        // Re-check the date is still inside the allowed window in case the
        // user left this page open across a cutoff before tapping Add.
        var window = SelectedProduct.MenuType == MenuType.Cycle ? 5 : 10;
        if (!_schedulingService.IsValidDeliveryDate(SelectedDeliveryDate.Date, window))
        {
            await AlertService.Instance.ShowAsync("Date no longer available", "That delivery date has passed the order cutoff. Please pick a new date.", "OK");
            await LoadDeliveryDatesAsync(SelectedProduct);
            return;
        }

        var selectedOptions = new List<Option>();
        foreach (var group in CustomizationGroups)
        {
            if (group.Options != null)
            {
                selectedOptions.AddRange(group.Options.Where(o => o.IsSelected));
            }
        }

        // FIXED: Snapshot/Clone the selected choices to isolate item states inside the basket container.
        // This stops subsequent menu configuration views from retroactively overriding things already in the cart.
        var cartOptionsSnapshot = selectedOptions.Select(o => new Option
        {
            Name = o.Name,
            AdditionalPrice = o.AdditionalPrice,
            IsSelected = true
        }).ToList();

        var item = new CartItem
        {
            Product = SelectedProduct,
            SelectedOptions = cartOptionsSnapshot,
            FinalPrice = TotalPrice,
            SpecialRequests = SpecialRequests,
            AllergyNotes = AllergyNotes,
            DeliveryDate = SelectedDeliveryDate.Date,
            MenuType = SelectedProduct.MenuType
        };

        _cartService.AddItem(item);

        // The core of the requested flow: after adding an item, ask whether
        // they want to keep ordering for the same day or switch days for
        // whatever they add next — rather than silently assuming either way.
        bool changeDay = await AlertService.Instance.ShowConfirmAsync(
            "Added to Basket",
            $"{SelectedProduct.Name} has been added for {SelectedDeliveryDate.DisplayLabel}.",
            "Order for a different day",
            $"Continue with {SelectedDeliveryDate.Date:dddd}");

        if (changeDay)
        {
            var services = Shell.Current.Handler?.MauiContext?.Services;
            var selectDayPage = services?.GetRequiredService<SelectDeliveryDayPage>();
            if (selectDayPage != null)
            {
                await Shell.Current.Navigation.PushAsync(selectDayPage);
                return; // stay here — don't also pop back to the menu underneath
            }
        }

        // Keep ordering for the same day — head back to the menu screen.
        await Shell.Current.GoToAsync("..");
    }
}