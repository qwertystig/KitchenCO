using System.Collections.Generic;

namespace YourKitchenCo.Models;

/// <summary>One category's hero banner + its dishes, for the sectioned dashboard list.</summary>
public class CategorySection
{
    public CategoryChip Category { get; set; } = new();
    public List<Product> Products { get; set; } = new();
}
