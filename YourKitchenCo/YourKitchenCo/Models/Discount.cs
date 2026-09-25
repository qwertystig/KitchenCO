using System;

namespace YourKitchenCo.Models;

/// <summary>
/// A promo/discount code, independent of the per-company subsidy/discount
/// already on <see cref="Company"/> — a company can have both a standing
/// subsidy and a code customers enter at checkout.
/// </summary>
public class Discount
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Code { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public bool Active { get; set; } = true;
    public DateTime? Expires { get; set; }

    /// <summary>Null/empty means the code applies to every company.</summary>
    public string? CompanyId { get; set; }
    /// <summary>Display-only company name for the "X only" badge.</summary>
    public string? CompanyName { get; set; }
}
