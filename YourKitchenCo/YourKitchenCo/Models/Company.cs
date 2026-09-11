using System;
using System.Collections.Generic;

namespace YourKitchenCo.Models;

public class Company
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string BillingEmail { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    /// <summary>Per-meal subsidy the company covers, VAT-inclusive. Applied per cart item at checkout.</summary>
    public decimal MealSubsidyAmount { get; set; }

    /// <summary>Discount applied to the order total, separate from the per-meal subsidy above — a company can have either, both, or neither.</summary>
    public DiscountType DiscountType { get; set; } = DiscountType.None;

    /// <summary>Meaning depends on DiscountType: a percentage (0-100) or a flat ZAR amount.</summary>
    public decimal DiscountValue { get; set; }

    /// <summary>
    /// Email domains (no "@", lowercase — e.g. "ecogra.org") that
    /// auto-match an employee to this company during registration. A
    /// company can have more than one (e.g. regional subsidiaries sharing
    /// one account). Registration still allows manual company selection
    /// for anyone whose domain isn't whitelisted — auto-match is a
    /// convenience, not a requirement, so nobody gets locked out of
    /// registering just because their company hasn't set this up yet.
    /// </summary>
    public List<string> WhitelistedDomains { get; set; } = new();

    public string FinancialsSummary
    {
        get
        {
            var parts = new List<string>();
            if (MealSubsidyAmount > 0) parts.Add($"R{MealSubsidyAmount:F2}/meal subsidy");
            if (DiscountType == DiscountType.Percentage && DiscountValue > 0) parts.Add($"{DiscountValue}% discount");
            if (DiscountType == DiscountType.FixedZar && DiscountValue > 0) parts.Add($"R{DiscountValue:F2} discount");
            return parts.Count > 0 ? string.Join(" · ", parts) : "No subsidy or discount set";
        }
    }
}

public enum DiscountType
{
    None,
    Percentage,
    FixedZar
}
