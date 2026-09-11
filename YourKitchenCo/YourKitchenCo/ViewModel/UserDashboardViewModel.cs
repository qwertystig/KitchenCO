using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using YourKitchenCo.Models;
using YourKitchenCo.Services;
using YourKitchenCo.Views;

namespace YourKitchenCo.ViewModel;

public partial class UserDashboardViewModel : ObservableObject
{
    private readonly IProductService _productService;
    private readonly ICartService _cartService;
    private readonly ISessionService _session;

    private List<Product> _masterProductList = new();

    private string _activeCategory = string.Empty;
    public string ActiveCategory
    {
        get => _activeCategory;
        set => SetProperty(ref _activeCategory, value);
    }

    [ObservableProperty]
    private int _cartCount;

    [ObservableProperty]
    private decimal _cartTotal;

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    private string _currentMenu = "Main";
    public string CurrentMenu
    {
        get => _currentMenu;
        set => SetProperty(ref _currentMenu, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilters();
            }
        }
    }

    private string _selectedDeliveryDateLabel = string.Empty;
    public string SelectedDeliveryDateLabel
    {
        get => _selectedDeliveryDateLabel;
        set => SetProperty(ref _selectedDeliveryDateLabel, value);
    }

    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<CategoryChip> Categories { get; } = new();
    public ObservableCollection<CategorySection> GroupedProducts { get; } = new();

    public UserDashboardViewModel(IProductService productService, ICartService cartService, ISessionService session)
    {
        _productService = productService;
        _cartService = cartService;
        _session = session;

        if (_cartService?.Items != null)
        {
            _cartService.Items.CollectionChanged += OnCartItemsChanged;
        }

        UpdateCartCount();
        UpdateSelectedDateLabel();
    }

    private void OnCartItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateCartCount();
    }

    public void UpdateCartCount()
    {
        CartCount = _cartService?.Items?.Sum(item => item.Quantity) ?? 0;
        CartTotal = _cartService?.Items?.Sum(item => item.FinalPrice) ?? 0m;
    }

    private void UpdateSelectedDateLabel()
    {
        SelectedDeliveryDateLabel = _session.SelectedOrderingDate.HasValue
            ? _session.SelectedOrderingDate.Value.ToString("dddd, dd MMM")
            : "Choose a delivery day";
    }

    [RelayCommand]
    private void FilterCategory(string category)
    {
        if (ActiveCategory == category)
        {
            ActiveCategory = string.Empty;
        }
        else
        {
            ActiveCategory = category;
        }

        ApplyFilters();
    }

    [RelayCommand]
    private void Search()
    {
        ApplyFilters();
    }

    [RelayCommand]
    private async Task SwitchToMainAsync()
    {
        CurrentMenu = "Main";
        ClearActiveFilters();
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task SwitchToCyclingAsync()
    {
        CurrentMenu = "Weekly";
        ClearActiveFilters();
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task NavigateToProductAsync(Product product)
    {
        if (product == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "SelectedProduct", product }
        };
        await Shell.Current.GoToAsync(nameof(ProductDetailPage), navigationParameter);
    }

    [RelayCommand]
    private async Task NavigateToCartAsync()
    {
        await Shell.Current.GoToAsync(nameof(CartPage));
    }

    /// <summary>Opens the full date picker — used by the compact date display so the person can change day without leaving the dashboard.</summary>
    [RelayCommand]
    private async Task ChangeDeliveryDayAsync()
    {
        var services = Shell.Current?.Handler?.MauiContext?.Services;
        var selectDayPage = services?.GetRequiredService<SelectDeliveryDayPage>();
        if (selectDayPage != null)
            await Shell.Current!.Navigation.PushAsync(selectDayPage);
    }

    public async Task LoadDataAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var items = await _productService.GetProductsAsync(CurrentMenu);

            _masterProductList = items?.ToList() ?? new List<Product>();

            RebuildCategories();
            ApplyFilters();
            UpdateSelectedDateLabel();
        }
        finally
        {
            // Guaranteed to run even if something above throws — otherwise
            // IsBusy gets stuck true forever and every future reload
            // silently no-ops on the guard at the top of this method,
            // which looks exactly like "the menu isn't updating."
            IsBusy = false;
        }
    }

    private void RebuildCategories()
    {
        Categories.Clear();

        var distinctCategories = _masterProductList
            .Where(p => !string.IsNullOrWhiteSpace(p.Category))
            .GroupBy(p => p.Category)
            .OrderBy(g => g.Key)
            .Select(g => new CategoryChip { Name = g.Key, Icon = g.First().Icon, ImageUrl = g.First().ImageUrl });

        foreach (var chip in distinctCategories)
            Categories.Add(chip);
    }

    private void ApplyFilters()
    {
        var filteredQuery = _masterProductList.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(ActiveCategory))
        {
            filteredQuery = filteredQuery.Where(p =>
                p.Category != null && p.Category.Equals(ActiveCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filteredQuery = filteredQuery.Where(p =>
                (p.Name != null && p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                (p.Description != null && p.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
        }

        var filteredList = filteredQuery.ToList();

        Products.Clear();
        foreach (var item in filteredList)
        {
            Products.Add(item);
        }

        // Grouped-by-category view, each with its own hero banner — mirrors
        // the reference app's CategorySection layout. Built from the same
        // filtered list and category order as the flat Products list above,
        // so search/category-filter/menu-switch all stay in sync without
        // needing separate logic.
        GroupedProducts.Clear();
        foreach (var category in Categories)
        {
            var productsInCategory = filteredList
                .Where(p => p.Category != null && p.Category.Equals(category.Name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (productsInCategory.Count > 0)
                GroupedProducts.Add(new CategorySection { Category = category, Products = productsInCategory });
        }
    }

    private void ClearActiveFilters()
    {
        SearchText = string.Empty;
        ActiveCategory = string.Empty;
    }
}
