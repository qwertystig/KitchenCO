namespace YourKitchenCo.Models;

/// <summary>
/// Static: always-available a-la-carte menu (staticMenu.json / the product xlsx).
///         Orderable up to 2 weeks (10 weekdays) in advance.
/// Cycle:  the 8-week rotating daily menu (cycleMenu.json), 5 categories per day.
///         Orderable across the coming week (5 weekdays) in advance, and each
///         day pulls that specific day's items.
/// </summary>
public enum MenuType
{
    Static,
    Cycle
}
