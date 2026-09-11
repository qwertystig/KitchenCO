using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

namespace YourKitchenCo.Models;

public class Order
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // Fixes 'Order' does not contain a definition for 'OrderId'
    public string OrderId { get; set; } = string.Empty;

    public string OrderNumber { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string SummaryText { get; set; } = string.Empty;

    // Distinct from SummaryText (general special requests) — a safety-critical
    // chef alert, matching CartItem.AllergyNotes.
    public string AllergyNotes { get; set; } = string.Empty;
    public bool HasAllergyNotes => !string.IsNullOrWhiteSpace(AllergyNotes);
    public string ItemName { get; set; } = string.Empty;

    // Snapshotted from Product.Category at checkout time — used for
    // reporting by category. Deliberately captured rather than looked up
    // against the current menu later, since a dish could be renamed,
    // recategorized, or removed entirely by the time a report runs.
    public string Category { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }
    public decimal SubsidyAmount { get; set; }

    // Flat per-checkout delivery fee (by distance tier — see
    // DeliveryFeeCalculator). Only recorded on the first order of a
    // checkout batch — see CartPageViewModel.CheckoutAsync — so it isn't
    // double-counted if orders are later summed per company/day.
    public decimal DeliveryFee { get; set; }

    // Customer's 1-5 star rating of this order, set after delivery. 0 = not rated yet.
    public int Rating { get; set; }
    public bool HasRating => Rating > 0;
    public string RatingFeedback { get; set; } = string.Empty;

    // Company discount (percentage or flat ZAR off the order), separate
    // from the per-meal subsidy — a company can have either, both, or
    // neither. Recorded per order the same way DeliveryFee is (first order
    // of a checkout batch only would double-count it, but unlike delivery
    // fee this applies per-item, so it's fine to record on every order).
    public decimal DiscountAmount { get; set; }

    // Auto-generated at checkout — "INV-KC-2609-0001" (INV-KC-YYMM-####).
    public string TaxInvoiceNumber { get; set; } = string.Empty;

    // Dispute — a customer can flag a delivered order with a reason;
    // generates a support ticket reference the same way the reference app
    // does ("TCK-88219"). Empty TicketRef means no dispute has been raised.
    public string DisputeReason { get; set; } = string.Empty;
    public string DisputeTicketRef { get; set; } = string.Empty;
    public DateTime? DisputeReportedAt { get; set; }
    public string DisputeStatus { get; set; } = string.Empty; // "Investigating" | "Refunded" | "Resolved"
    public bool HasDispute => !string.IsNullOrWhiteSpace(DisputeTicketRef);

    // 4-stage delivery tracker, matching the reference app's model —
    // derived from Status rather than stored separately, so there's one
    // source of truth and admin's existing status buttons don't need to
    // also remember to update a second field.
    public int Stage => Status switch
    {
        "Received" => 1,
        "Preparing" => 2,
        "Out for Delivery" => 3,
        "Delivered" => 4,
        _ => 1
    };

    // Reorder only makes sense for the always-available static menu — a
    // cycle-menu dish is locked to one specific day and likely isn't even
    // on the menu anymore by the time someone looks at their order history.
    public bool CanReorder => MenuType == MenuType.Static;

    /// <summary>
    /// ItemName is stored as a display string like "2x Classic Burger -
    /// Beef" rather than separate dish/quantity fields — this recovers
    /// both. Shared here rather than duplicated wherever it's needed
    /// (reorder lookup, kitchen prep aggregation).
    /// </summary>
    public (string DishName, int Quantity) ParseItemNameAndQuantity()
    {
        var separatorIndex = ItemName.IndexOf("x ", StringComparison.Ordinal);
        if (separatorIndex > 0 && separatorIndex <= 3 && int.TryParse(ItemName[..separatorIndex], out var qty))
            return (ItemName[(separatorIndex + 2)..], qty);

        return (ItemName, 1);
    }

    // Added for order-window / cycle-menu scheduling
    public DateOnly DeliveryDate { get; set; }
    public MenuType MenuType { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
    public string DeliveryFloor { get; set; } = string.Empty;
}