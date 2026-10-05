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

public partial class AdminMenuViewModel : ObservableObject
{
    private readonly IProductService _productService;
    private readonly ICycleMenuService _cycleMenuService;
    private ObservableCollection<Product> _allProducts = new();

    // ---- Static menu state ----
    public ObservableCollection<Product> Products { get; } = new();

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    // ---- Menu type toggle ----
    [ObservableProperty]
    private string _currentMenuType = "Static"; // "Static" or "Cycle"

    // ---- Cycle menu state ----
    public ObservableCollection<string> WeekOptions { get; } = new()
    {
        "Week 1", "Week 2", "Week 3", "Week 4", "Week 5", "Week 6", "Week 7", "Week 8"
    };

    [ObservableProperty]
    private string _selectedWeek = "Week 1";

    [ObservableProperty]
    private int _activeWeekNumber;

    public ObservableCollection<CycleMenuItemRow> CycleItems { get; } = new();

    public AdminMenuViewModel(IProductService productService, ICycleMenuService cycleMenuService)
    {
        _productService = productService;
        _cycleMenuService = cycleMenuService;
        _ = LoadProductsAsync();
        _ = LoadActiveWeekAsync();
    }

    partial void OnSearchQueryChanged(string value) => ApplyFilter();
    partial void OnSelectedWeekChanged(string value) => _ = LoadCycleItemsForSelectedWeekAsync();

    // ============ MENU TYPE SWITCHING ============

    [RelayCommand]
    private void SwitchToStatic() => CurrentMenuType = "Static";

    [RelayCommand]
    private async Task SwitchToCycleAsync()
    {
        CurrentMenuType = "Cycle";
        if (CycleItems.Count == 0)
            await LoadCycleItemsForSelectedWeekAsync();
    }

    // ============ STATIC MENU CRUD ============

    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        IsBusy = true;
        try
        {
            var items = await _productService.GetProductsAsync("Main");
            _allProducts = new ObservableCollection<Product>(items);
            ApplyFilter();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilter()
    {
        Products.Clear();
        var query = _allProducts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            query = query.Where(p => p.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)
                                 || p.Description.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in query)
        {
            Products.Add(item);
        }
    }

    [RelayCommand]
    private async Task AddProductAsync()
    {
        if (Application.Current?.MainPage == null) return;

        string name = await AlertService.Instance.ShowPromptAsync("New Menu Item", "Enter item name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        string priceStr = await AlertService.Instance.ShowPromptAsync("Price", "Enter base price:", keyboard: Keyboard.Numeric);
        if (!decimal.TryParse(priceStr, out decimal price)) price = 9.99m;

        string category = await AlertService.Instance.ShowPromptAsync("Category", "Enter category (e.g. BURGER BAR):", initialValue: "MISC");
        var categoryUpper = string.IsNullOrWhiteSpace(category) ? "MISC" : category.ToUpperInvariant();

        string description = await AlertService.Instance.ShowPromptAsync("Description", "Short description shown to customers:", initialValue: "Freshly prepared gourmet selection.");

        string ingredients = await AlertService.Instance.ShowPromptAsync("Ingredients", "Comma-separated list, shown on the dish's listing (optional):");

        string dietaryTagsStr = await AlertService.Instance.ShowPromptAsync("Dietary Tags", "Comma-separated, e.g. \"Vegetarian, Gluten-Free\" (optional):");
        var dietaryTags = string.IsNullOrWhiteSpace(dietaryTagsStr)
            ? new List<string>()
            : dietaryTagsStr.Split(',').Select(t => t.Trim()).Where(t => t.Length > 0).ToList();

        var newProduct = new Product
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(description) ? "Freshly prepared gourmet selection." : description,
            Ingredients = ingredients ?? string.Empty,
            DietaryTags = dietaryTags,
            BasePrice = price,
            Category = categoryUpper,
            IsAvailable = true,
            Icon = "🍽️",
            // Sized to match JsonProductService's dish images (was 800x600 —
            // far larger than this ever renders at, same unnecessary decode
            // cost noted in the ANR fix).
            ImageUrl = "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=500&h=360&fit=crop&auto=format&q=80"
        };

        // Persisted through the shared service, not just this screen's local
        // list — so it actually shows up on the customer dashboard too.
        await _productService.AddStaticProductAsync(newProduct);

        _allProducts.Insert(0, newProduct);
        ApplyFilter();

        await AlertService.Instance.ShowAsync("Product Added", $"{newProduct.Name} has been added to the catalog.", "OK");
    }

    [RelayCommand]
    private async Task ToggleAvailabilityAsync(Product product)
    {
        if (product == null) return;

        product.IsAvailable = !product.IsAvailable;
        await _productService.UpdateStaticProductAsync(product);
        ApplyFilter(); // Refresh view state

        string state = product.IsAvailable ? "available" : "out of stock";
        if (Application.Current?.MainPage != null)
        {
            await AlertService.Instance.ShowAsync("Inventory Updated", $"{product.Name} is now marked as {state}.", "OK");
        }
    }

    [RelayCommand]
    private async Task EditProductAsync(Product product)
    {
        if (product == null || Application.Current?.MainPage == null) return;

        string updatedName = await AlertService.Instance.ShowPromptAsync("Edit Item", "Update item name:", initialValue: product.Name);
        if (string.IsNullOrWhiteSpace(updatedName)) return;

        string updatedPriceStr = await AlertService.Instance.ShowPromptAsync("Edit Price", "Update base price:", initialValue: product.BasePrice.ToString("F2"), keyboard: Keyboard.Numeric);
        if (!decimal.TryParse(updatedPriceStr, out decimal updatedPrice)) return;

        string updatedDescription = await AlertService.Instance.ShowPromptAsync("Edit Description", "Short description shown to customers:", initialValue: product.Description);

        string updatedIngredients = await AlertService.Instance.ShowPromptAsync("Edit Ingredients", "Comma-separated list (optional):", initialValue: product.Ingredients);

        string updatedTagsStr = await AlertService.Instance.ShowPromptAsync("Edit Dietary Tags", "Comma-separated, e.g. \"Vegetarian, Gluten-Free\" (optional):", initialValue: string.Join(", ", product.DietaryTags));

        product.Name = updatedName;
        product.BasePrice = updatedPrice;
        product.Description = updatedDescription ?? string.Empty;
        product.Ingredients = updatedIngredients ?? string.Empty;
        product.DietaryTags = string.IsNullOrWhiteSpace(updatedTagsStr)
            ? new List<string>()
            : updatedTagsStr.Split(',').Select(t => t.Trim()).Where(t => t.Length > 0).ToList();

        await _productService.UpdateStaticProductAsync(product);
        ApplyFilter();
    }

    [RelayCommand]
    private async Task DeleteProductAsync(Product product)
    {
        if (product == null || Application.Current?.MainPage == null) return;

        bool confirm = await AlertService.Instance.ShowConfirmAsync("Confirm Delete", $"Are you sure you want to remove '{product.Name}'?", "Delete", "Cancel");
        if (confirm)
        {
            await _productService.DeleteStaticProductAsync(product.Id);
            _allProducts.Remove(product);
            ApplyFilter();
        }
    }

    // ============ CYCLE MENU: ACTIVE WEEK ============

    private async Task LoadActiveWeekAsync()
    {
        ActiveWeekNumber = await _cycleMenuService.GetActiveWeekNumberAsync();
        SelectedWeek = $"Week {ActiveWeekNumber}";
    }

    [RelayCommand]
    private async Task SetActiveWeekAsync()
    {
        if (Application.Current?.MainPage == null) return;
        if (!int.TryParse(SelectedWeek.Replace("Week ", ""), out var weekNumber)) return;

        bool confirm = await AlertService.Instance.ShowConfirmAsync(
            "Activate this week?",
            $"Make {SelectedWeek} the active menu for the current cycle? Future weeks will keep rotating normally from here.",
            "Activate", "Cancel");

        if (!confirm) return;

        await _cycleMenuService.SetActiveWeekAsync(weekNumber);
        ActiveWeekNumber = await _cycleMenuService.GetActiveWeekNumberAsync();

        await AlertService.Instance.ShowAsync("Active Week Updated", $"{SelectedWeek} is now active for this week's deliveries.", "OK");
    }

    // ============ CYCLE MENU: BROWSE / EDIT ITEMS ============

    private async Task LoadCycleItemsForSelectedWeekAsync()
    {
        IsBusy = true;
        try
        {
            var allWeeks = await _cycleMenuService.GetAllWeeksAsync();
            CycleItems.Clear();

            if (!allWeeks.TryGetValue(SelectedWeek, out var days)) return;

            foreach (var day in days)
            {
                foreach (var (category, item) in day.Categories)
                {
                    CycleItems.Add(new CycleMenuItemRow
                    {
                        WeekKey = SelectedWeek,
                        DayName = day.Day,
                        CategoryLabel = category,
                        Value = item
                    });
                }
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EditCycleItemAsync(CycleMenuItemRow row)
    {
        if (row == null || Application.Current?.MainPage == null) return;

        string updated = await AlertService.Instance.ShowPromptAsync(
            $"Edit {row.DayName} — {row.CategoryLabel}",
            "Update the dish:",
            initialValue: row.Value);

        if (string.IsNullOrWhiteSpace(updated) || updated == row.Value) return;

        await _cycleMenuService.UpdateCycleItemAsync(row.WeekKey, row.DayName, row.CategoryLabel, updated);

        row.Value = updated;
        // Force the CollectionView to re-read the row (Product/CycleMenuItemRow
        // aren't ObservableObjects, so a manual refresh keeps this simple).
        var index = CycleItems.IndexOf(row);
        if (index >= 0)
        {
            CycleItems.RemoveAt(index);
            CycleItems.Insert(index, row);
        }
    }

    /// <summary>
    /// Removes a dish from the cycle menu — the day/category grid itself is
    /// fixed (every day always has exactly 5 category slots), so "delete"
    /// clears the slot's dish rather than removing the row. An empty slot
    /// then shows "Add Dish" instead of "Edit" (see CycleMenuItemRow), and
    /// tapping it goes through the same EditCycleItemAsync above — typing
    /// something into an empty slot is effectively "add".
    /// </summary>
    [RelayCommand]
    private async Task DeleteCycleItemAsync(CycleMenuItemRow row)
    {
        if (row == null || Application.Current?.MainPage == null) return;

        if (string.IsNullOrWhiteSpace(row.Value))
        {
            await AlertService.Instance.ShowAsync("Already Empty", "This slot has no dish to remove.", "OK");
            return;
        }

        bool confirm = await AlertService.Instance.ShowConfirmAsync(
            "Remove Dish?",
            $"Remove \"{row.Value}\" from {row.DayName} — {row.CategoryLabel}? The slot will be empty until a new dish is added.",
            "Remove", "Cancel");

        if (!confirm) return;

        await _cycleMenuService.UpdateCycleItemAsync(row.WeekKey, row.DayName, row.CategoryLabel, string.Empty);

        row.Value = string.Empty;
        var index = CycleItems.IndexOf(row);
        if (index >= 0)
        {
            CycleItems.RemoveAt(index);
            CycleItems.Insert(index, row);
        }
    }
}
