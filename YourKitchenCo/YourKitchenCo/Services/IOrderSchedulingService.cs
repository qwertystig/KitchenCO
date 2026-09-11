namespace YourKitchenCo.Services;

/// <summary>
/// Encapsulates the company-wide ordering & delivery window rules:
///   - Ordering is open every day, including weekends — a weekend order
///     simply rolls forward to the next Monday for cutoff/business-day
///     purposes, which (via the 2-business-day rule below) lands on
///     Wednesday delivery. There's no need to special-case "weekend order"
///     separately from "Monday order after the 9am cutoff" — they're the
///     same calculation.
///   - Delivery date = 2 business days after the effective order date
///     (Mon -> Wed, Tue -> Thu, Wed -> Fri, Thu -> next Mon, Fri -> next Tue,
///     Sat/Sun -> next Mon -> Wed).
///   - An optional same-day cutoff time, after which "today" no longer
///     counts as a valid order date and everything rolls to the next
///     weekday.
///
/// IMPORTANT: This logic must also be enforced server-side (a Postgres
/// check constraint or a Supabase Edge Function validating the order
/// before it's accepted). This client-side service controls what the UI
/// shows and pre-validates, but a modified client could otherwise submit
/// an order for an invalid date.
/// </summary>
public interface IOrderSchedulingService
{
    /// <summary>
    /// Always true — ordering is open every day of the week, including
    /// weekends (customers just land on Wednesday as their earliest
    /// delivery option, same as ordering Monday after the cutoff would).
    /// Kept as a method rather than removed outright so a future business
    /// rule change (e.g. a public holiday closure) has somewhere to live
    /// without re-threading every call site again.
    /// </summary>
    bool IsOrderingOpen(DateTime? now = null);

    /// <summary>
    /// The order date to treat "right now" as: today, if it's a weekday and
    /// before the cutoff time; otherwise the next available weekday.
    /// </summary>
    DateOnly GetEffectiveOrderDate(DateTime? now = null);

    /// <summary>Maps a given order date to its delivery date under the 2-business-day rule.</summary>
    DateOnly GetDeliveryDateForOrderDate(DateOnly orderDate);

    /// <summary>The earliest date a customer could get food delivered if they ordered right now.</summary>
    DateOnly GetNextAvailableDeliveryDate(DateTime? now = null);

    /// <summary>
    /// The list of delivery dates a customer is currently allowed to order for,
    /// starting from the next available date. Use windowInWeekdays = 5 for the
    /// cycle menu (order across the coming week) and 10 for the static menu
    /// (order up to 2 weeks ahead).
    /// </summary>
    List<DateOnly> GetOrderableDeliveryDates(int windowInWeekdays, DateTime? now = null);

    /// <summary>
    /// Defensive re-check before submitting an order: confirms the requested
    /// delivery date is still inside the currently allowed window. Call this
    /// again at checkout time, not just when the date picker was first shown,
    /// in case the user left the app open across a cutoff.
    /// </summary>
    bool IsValidDeliveryDate(DateOnly requestedDeliveryDate, int windowInWeekdays, DateTime? now = null);
}
