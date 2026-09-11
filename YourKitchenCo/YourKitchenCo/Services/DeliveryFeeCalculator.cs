namespace YourKitchenCo.Services;

/// <summary>
/// Delivery fee tiers, by straight-line distance from the kitchen:
///   0     – 15km   → R100
///   15.01 – 20km   → R140
///   20.01 – 30km   → R200
///   30.01 – 50km   → R350
///
/// Anything beyond 50km isn't a defined tier in the business rules given —
/// rather than silently guessing a price, this returns null so the caller
/// can flag it (e.g. "outside delivery range, contact us") instead of
/// charging a made-up fee.
/// </summary>
public static class DeliveryFeeCalculator
{
    public static decimal? GetFee(decimal distanceKm)
    {
        if (distanceKm <= 15m) return 100m;
        if (distanceKm <= 20m) return 140m;
        if (distanceKm <= 30m) return 200m;
        if (distanceKm <= 50m) return 350m;
        return null; // outside the defined delivery range
    }

    public static string GetTierLabel(decimal distanceKm)
    {
        if (distanceKm <= 15m) return "0–15km";
        if (distanceKm <= 20m) return "15.01–20km";
        if (distanceKm <= 30m) return "20.01–30km";
        if (distanceKm <= 50m) return "30.01–50km";
        return "Outside delivery range (50km+)";
    }
}
