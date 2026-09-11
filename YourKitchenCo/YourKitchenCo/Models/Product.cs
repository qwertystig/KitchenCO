using System.Collections.Generic;
using System.Linq;

namespace YourKitchenCo.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }

    // Updated to use the correct model type for your UI
    public string ImageUrl { get; set; } = "https://via.placeholder.com/150";

    // Vivid, detailed emoji shown instead of a photo (e.g. "🍔") — chosen per
    // category for accuracy and color, not just placeholder cuteness. See
    // JsonProductService for the full category → emoji mapping.
    public string Icon { get; set; } = "🍽️";

    // Menu section this item belongs to — an xlsx/JSON category name for
    // static items ("BURGER BAR"), or the meal slot for cycle items
    // ("Curry of the Day"). Used to build the dashboard's category chips.
    public string Category { get; set; } = string.Empty;

    // Matches the reference app's data model — not sourced from real data
    // yet (staticMenu.json doesn't have this), so these stay empty for now.
    // The list-view UI hides these sections entirely when empty rather than
    // showing a blank line, same as the reference does.
    public string Ingredients { get; set; } = string.Empty;
    public List<string> DietaryTags { get; set; } = new();
    public bool HasIngredients => !string.IsNullOrWhiteSpace(Ingredients);
    public bool HasDietaryTags => DietaryTags.Count > 0;

    // For the list view's "Large R140.00" sub-price line — pulled from the
    // existing Size customization group rather than a separate field, so
    // there's one source of truth for pricing.
    public decimal? LargePrice
    {
        get
        {
            var sizeGroup = CustomizationGroups.FirstOrDefault(g => g.Title == "Size");
            var largeOption = sizeGroup?.Options.FirstOrDefault(o => o.Name == "Large");
            return largeOption is null ? null : BasePrice + largeOption.AdditionalPrice;
        }
    }

    // Set only for cycle-menu items: this dish only exists on this one date,
    // so ProductDetailPage skips the multi-date picker and locks to it.
    public DateOnly? LockedDeliveryDate { get; set; }

    // Required for Admin Menu Inventory toggle
    public bool IsAvailable { get; set; } = true;

    // Static = always-available a-la-carte menu (order up to 2 weeks ahead)
    // Cycle  = the 8-week rotating daily menu (order across the coming week)
    public MenuType MenuType { get; set; } = MenuType.Static;

    // This collection is what the ViewModel uses to populate the UI
    public List<CustomizationGroup> CustomizationGroups { get; set; } = new();
}