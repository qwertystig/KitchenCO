using System.Collections;
using System.Collections.Generic;

namespace YourKitchenCo.Models;

/// <summary>
/// One category's hero banner + its dishes, for the sectioned dashboard list.
/// Implements IEnumerable&lt;Product&gt; so it can serve directly as a
/// CollectionView group — CollectionView's IsGrouped="True" requires each
/// group object in the ItemsSource to itself be enumerable over its items.
/// This is what lets the dashboard's product list be a virtualized, grouped
/// CollectionView instead of a non-virtualizing BindableLayout (the latter
/// instantiated every category's every dish view all at once, synchronously,
/// on the main thread — the root cause of the app-wide freeze/ANR reported
/// after login).
/// </summary>
public class CategorySection : IEnumerable<Product>
{
    public CategoryChip Category { get; set; } = new();
    public List<Product> Products { get; set; } = new();

    public IEnumerator<Product> GetEnumerator() => Products.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Products.GetEnumerator();
}
