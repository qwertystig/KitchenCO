using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using YourKitchenCo.Models;

namespace YourKitchenCo.Services;

/// <summary>
/// Real menu data source, replacing MockProductService. Reads two bundled
/// MauiAssets:
///   - staticMenu.json: the always-available a-la-carte menu (categories,
///     icons, items with standard/large prices and descriptions)
///   - cycleMenu.json: the 8-week rotating daily menu (via ICycleMenuService)
///
/// "Main" (or any category other than "Weekly") returns the static menu.
/// "Weekly" returns the cycle menu items for the current orderable window
/// (the coming 5 weekdays), each one locked to its specific delivery date
/// since the dish only exists on that day.
///
/// This is still a local-file stand-in for the eventual Supabase tables —
/// nothing that consumes IProductService needs to change when that happens.
/// </summary>
public class JsonProductService : IProductService
{
    private const string StaticMenuFileName = "staticMenu.json";

    private readonly IOrderSchedulingService _schedulingService;
    private readonly ICycleMenuService _cycleMenuService;
    private readonly ISessionService _session;

    private List<Product>? _staticProductsCache;

    public JsonProductService(IOrderSchedulingService schedulingService, ICycleMenuService cycleMenuService, ISessionService session)
    {
        _schedulingService = schedulingService;
        _cycleMenuService = cycleMenuService;
        _session = session;
    }

    public async Task<List<Product>> GetProductsAsync() => await GetProductsAsync("Main");

    public async Task<List<Product>> GetProductsAsync(string category)
    {
        return category switch
        {
            "Weekly" => await GetCycleMenuProductsAsync(),
            _ => await GetStaticMenuProductsAsync()
        };
    }

    private async Task<List<Product>> GetStaticMenuProductsAsync()
    {
        if (_staticProductsCache is not null)
            return _staticProductsCache;

        using var stream = await FileSystem.OpenAppPackageFileAsync(StaticMenuFileName);
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        var products = new List<Product>();
        var id = 1;

        foreach (var categoryProp in root.EnumerateObject())
        {
            if (categoryProp.Name == "_icons") continue;

            var categoryName = categoryProp.Name;
            var icon = StaticCategoryIcon(categoryName);
            var categoryImageUrl = StaticCategoryImageUrl(categoryName);

            // Category-level add-ons (e.g. "Extra Cheese", "Potato Wedges")
            // apply to every dish in the category — built once per category,
            // then a fresh copy (with its own IsSelected state) is attached
            // to each product below.
            var addOnOptions = new List<(string Name, decimal Price)>();
            if (categoryProp.Value.TryGetProperty("addOns", out var addOnsEl) && addOnsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var addOnEl in addOnsEl.EnumerateArray())
                {
                    var addOnName = addOnEl.TryGetProperty("name", out var anEl) ? anEl.GetString() ?? "" : "";
                    var addOnPriceStr = addOnEl.TryGetProperty("price", out var apEl) ? apEl.GetString() : null;
                    var addOnPrice = addOnPriceStr is not null ? ParsePrice(addOnPriceStr) ?? 0m : 0m;
                    if (!string.IsNullOrWhiteSpace(addOnName))
                        addOnOptions.Add((addOnName, addOnPrice));
                }
            }

            if (!categoryProp.Value.TryGetProperty("items", out var itemsEl) || itemsEl.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var itemElement in itemsEl.EnumerateArray())
            {
                var name = itemElement.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "Menu Item" : "Menu Item";
                var imageUrl = StaticItemImageUrl(categoryName, name, categoryImageUrl);
                var description = itemElement.TryGetProperty("description", out var descEl) ? descEl.GetString() ?? "" : "";
                var ingredients = itemElement.TryGetProperty("ingredients", out var ingEl) ? ingEl.GetString() ?? "" : "";

                var dietaryTags = new List<string>();
                if (itemElement.TryGetProperty("dietaryTags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var tagEl in tagsEl.EnumerateArray())
                        if (tagEl.GetString() is string tag && !string.IsNullOrWhiteSpace(tag))
                            dietaryTags.Add(tag);
                }

                var priceStrings = new List<string>();
                if (itemElement.TryGetProperty("prices", out var pricesEl) && pricesEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var p in pricesEl.EnumerateArray())
                        if (p.GetString() is string s) priceStrings.Add(s);
                }
                else if (itemElement.TryGetProperty("price", out var priceEl) && priceEl.GetString() is string single)
                {
                    priceStrings.Add(single);
                }

                var parsedPrices = priceStrings.Select(ParsePrice).Where(p => p.HasValue).Select(p => p!.Value).ToList();
                var standardPrice = parsedPrices.Count > 0 ? parsedPrices[0] : 0m;
                var largePrice = parsedPrices.Count > 1 ? parsedPrices[1] : (decimal?)null;

                var product = new Product
                {
                    Id = id++,
                    Name = name,
                    Description = string.IsNullOrWhiteSpace(description)
                        ? "A perfect addition to complement your meal."
                        : description,
                    BasePrice = standardPrice,
                    Icon = icon,
                    ImageUrl = imageUrl,
                    Category = categoryName,
                    Ingredients = ingredients,
                    DietaryTags = dietaryTags,
                    MenuType = MenuType.Static
                };

                // If there's a second (large) price, expose it as a real Size
                // customization option rather than throwing the data away.
                if (largePrice.HasValue && largePrice.Value > standardPrice)
                {
                    product.CustomizationGroups.Add(new CustomizationGroup
                    {
                        Title = "Size",
                        IsMultiSelect = false,
                        Options = new List<Option>
                        {
                            new Option { Name = "Standard", AdditionalPrice = 0m, IsSelected = true },
                            new Option { Name = "Large", AdditionalPrice = largePrice.Value - standardPrice }
                        }
                    });
                }

                // Category-level add-ons (extras, sauces, sides) — a fresh
                // set of Option instances per product so each product's
                // selections are independent.
                if (addOnOptions.Count > 0)
                {
                    product.CustomizationGroups.Add(new CustomizationGroup
                    {
                        Title = "Add-Ons",
                        IsMultiSelect = true,
                        Options = addOnOptions
                            .Select(a => new Option { Name = a.Name, AdditionalPrice = a.Price })
                            .ToList()
                    });
                }

                products.Add(product);
            }
        }

        _staticProductsCache = products;
        return products;
    }

    private async Task<List<Product>> GetCycleMenuProductsAsync()
    {
        var allOrderableDates = _schedulingService.GetOrderableDeliveryDates(5);

        // If the person has already picked a delivery day (at login, or via
        // the "order for a different day" prompt), show only that day's
        // dishes — the cycling menu should match the day they're actually
        // ordering for, not the whole week at once. Falls back to the full
        // week if their chosen date has fallen outside the window for some
        // reason (e.g. session held open across a cutoff).
        var selectedDate = _session.SelectedOrderingDate;
        var dates = selectedDate.HasValue && allOrderableDates.Contains(selectedDate.Value)
            ? new List<DateOnly> { selectedDate.Value }
            : allOrderableDates;

        var products = new List<Product>();
        var id = 1000;

        foreach (var date in dates)
        {
            var day = await _cycleMenuService.GetMenuForDateAsync(date);
            if (day is null) continue;

            foreach (var (categoryLabel, itemName) in day.Categories)
            {
                if (string.IsNullOrWhiteSpace(itemName)) continue;

                products.Add(new Product
                {
                    Id = id++,
                    Name = itemName,
                    Description = $"{categoryLabel} — delivered {date:dddd, dd MMM}. Part of week {day.WeekNumber}'s rotating menu.",
                    BasePrice = 80.00m, // real price, confirmed in the client's product-list spreadsheet (was a R95 placeholder)
                    Icon = CycleCategoryIcon(categoryLabel),
                    ImageUrl = CycleCategoryImageUrl(categoryLabel),
                    Category = categoryLabel,
                    MenuType = MenuType.Cycle,
                    LockedDeliveryDate = date
                });
            }
        }

        return products;
    }

    /// <summary>
    /// Vivid, detailed emoji per static-menu category — deliberately not the
    /// generic suggestions from staticMenu.json's own "_icons" map, chosen
    /// instead for how accurately and colorfully each one represents the
    /// category (e.g. an actual ramen bowl emoji, not a generic bowl).
    /// </summary>
    private static string StaticCategoryIcon(string categoryName) => categoryName.ToUpperInvariant() switch
    {
        "SALAD BAR" => "🥗",
        "POKE BOWL" => "🍱",
        "STIR FRY" => "🥘",
        "CIAO ITALY" => "🍝",
        "WRAPS" => "🌯",
        "SANDWICHES" => "🥪",
        "HOT DOGS" => "🌭",
        "BURGER BAR" => "🍔",
        "FITNESS MEALS" => "🥑",
        "HOMEMADE WINTER SOUPS" => "🍲",
        "RAMEN BOWLS" => "🍜",
        "VEGAN MEALS" => "🥦",
        "PORK SPECIALITIES" => "🐷",
        _ => "🍽️"
    };

    private static string CycleCategoryIcon(string categoryLabel) => categoryLabel switch
    {
        "Main Meal" => "🍽️",
        "Vegetarian Meal" => "🥕",
        "Healthy Meal" => "🍎",
        "Curry of the Day" => "🍛",
        "Gourmet Sandwich" => "🥪",
        _ => "🍽️"
    };

    /// <summary>
    /// Real photography per static-menu category — non-copyright, sourced
    /// from Unsplash (Unsplash License: free for commercial use, no
    /// attribution required — see https://unsplash.com/license). Each URL
    /// points at a specific photo's stable CDN address, not a search/random
    /// endpoint, so the same image loads every time.
    /// </summary>
    /// <summary>
    /// Admin-set hero image for a category not covered by the hardcoded
    /// mapping below — e.g. a brand new category created via "Add Menu
    /// Item" with a category name that didn't exist before. Checked first;
    /// falls through to the switch below for every category that already
    /// has a curated photo.
    /// </summary>
    private static readonly Dictionary<string, string> CustomCategoryImages = new(StringComparer.OrdinalIgnoreCase);

    public static void SetCustomCategoryImage(string categoryName, string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(categoryName)) return;
        CustomCategoryImages[categoryName] = imageUrl;
    }

    private static string StaticCategoryImageUrl(string categoryName)
    {
        if (CustomCategoryImages.TryGetValue(categoryName, out var customUrl))
            return customUrl;

        var photoId = categoryName.ToUpperInvariant() switch
        {
            "SALAD BAR" => "1512621776951-a57141f2eefd",
            "POKE BOWL" => "1759429179911-4e1f0e4e69f7",
            "STIR FRY" => "1751560048567-b38f5cf3448a",
            "CIAO ITALY" => "1755594461640-b800c6bafdfa",
            "WRAPS" => "1571331421405-51b8feea1033",
            "SANDWICHES" => "1469648034646-7911874fe62b",
            "HOT DOGS" => "1638368593249-7cadb261e8b3",
            "BURGER BAR" => "1667329829058-ac191ba4a905",
            "FITNESS MEALS" => "1761027101409-fa96d88349c7",
            "HOMEMADE WINTER SOUPS" => "1631531515768-cb84c3bd98f0",
            "RAMEN BOWLS" => "1711394370771-817a30b06215",
            "VEGAN MEALS" => "1654199903998-e49181b41a95",
            "PORK SPECIALITIES" => "1659415401946-bbee8c483e83", // pork chop, mashed potato & gravy — confirmed Unsplash-licensed
            _ => "1512621776951-a57141f2eefd" // fallback: salad bowl
        };

        return $"https://images.unsplash.com/photo-{photoId}?w=800&h=600&fit=crop&auto=format&q=80";
    }

    /// <summary>
    /// Exact per-item photo overrides, built up incrementally (one accurate
    /// photo per dish, sourced individually — see CHANGELOG for progress).
    /// Keyed by the exact item name as it appears in staticMenu.json,
    /// case-insensitive. Checked before the category-level fallback below.
    /// </summary>
    private static readonly Dictionary<string, string> ItemPhotoIds = new(StringComparer.OrdinalIgnoreCase)
    {
        // SALAD BAR
        ["Tuna Salad"] = "1514518189759-94d8ee01ecf1",
        ["Roasted Butternut Salad"] = "1623428188474-b1d532c5e560",
        ["Grilled Chicken Salad"] = "1505714197102-6ae95091ed70",
        ["Grilled Haloumi Salad"] = "1505714197102-6ae95091ed70", // shares the chicken/pine-nut salad photo — same salad base, different protein
        ["Thai Beef Noodle Salad"] = "1673258551460-268b47871a32",
        ["Roasted Vegetable and Couscous Salad"] = "1623428188474-b1d532c5e560", // shares the grain/veg bowl above — genuinely very similar dish
    };

    /// <summary>
    /// Per-item photo lookup. "SIDES & SAUCES" used to be its own browsable
    /// category (handled here with sauce/salad keyword matching) but is now
    /// folded into every other category's Add-Ons instead — add-ons are
    /// Options, not Products, so they don't need their own photo. Falls back
    /// to the category photo for anything not specifically overridden above.
    /// </summary>
    private static string StaticItemImageUrl(string categoryName, string itemName, string categoryFallbackUrl)
    {
        if (ItemPhotoIds.TryGetValue(itemName, out var photoId))
            return $"https://images.unsplash.com/photo-{photoId}?w=800&h=600&fit=crop&auto=format&q=80";

        return categoryFallbackUrl;
    }
    private static string CycleCategoryImageUrl(string categoryLabel)
    {
        var photoId = categoryLabel switch
        {
            "Main Meal" => "1635897411141-7bd2b9c6ab16",       // roast plate — a proper hearty home-style main
            "Vegetarian Meal" => "1654199903998-e49181b41a95", // vegan bowl
            "Healthy Meal" => "1512621776951-a57141f2eefd",    // salad bowl
            "Curry of the Day" => "1708782344490-9026aaa5eec7",// chicken curry with rice
            "Gourmet Sandwich" => "1469648034646-7911874fe62b",// club sandwich
            _ => "1512621776951-a57141f2eefd"
        };

        return $"https://images.unsplash.com/photo-{photoId}?w=800&h=600&fit=crop&auto=format&q=80";
    }

    public async Task<Product> AddStaticProductAsync(Product product)
    {
        await GetStaticMenuProductsAsync(); // ensures _staticProductsCache is loaded

        if (product.Id <= 0)
            product.Id = _staticProductsCache!.Count == 0 ? 1 : _staticProductsCache.Max(p => p.Id) + 1;

        product.MenuType = MenuType.Static;
        _staticProductsCache!.Insert(0, product);
        return product;
    }

    public async Task UpdateStaticProductAsync(Product product)
    {
        await GetStaticMenuProductsAsync();

        var existing = _staticProductsCache!.FirstOrDefault(p => p.Id == product.Id);
        if (existing is null) return;

        existing.Name = product.Name;
        existing.Description = product.Description;
        existing.Ingredients = product.Ingredients;
        existing.DietaryTags = product.DietaryTags;
        existing.BasePrice = product.BasePrice;
        existing.Category = product.Category;
        existing.Icon = product.Icon;
        existing.ImageUrl = product.ImageUrl;
        existing.IsAvailable = product.IsAvailable;
    }

    public async Task DeleteStaticProductAsync(int productId)
    {
        await GetStaticMenuProductsAsync();

        var existing = _staticProductsCache!.FirstOrDefault(p => p.Id == productId);
        if (existing is not null)
            _staticProductsCache!.Remove(existing);
    }

    private static decimal? ParsePrice(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var cleaned = raw.Replace("R", "", StringComparison.OrdinalIgnoreCase).Trim();
        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }
}
