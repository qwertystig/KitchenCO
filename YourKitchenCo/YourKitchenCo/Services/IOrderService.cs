using System.Collections.Generic;
using System.Threading.Tasks;
using YourKitchenCo.Models;

namespace YourKitchenCo.Services;

public interface IOrderService
{
    /// <summary>Adds an order to the store. Assigns Id/OrderNumber if not already set.</summary>
    Task<Order> PlaceOrderAsync(Order order);

    /// <summary>
    /// Converts a cart's items into real Order records — one order per cart
    /// line for now (see the implementation for why), applying the
    /// company's per-meal subsidy and a single flat delivery fee charged
    /// only once per checkout. Shared between the cart's own checkout
    /// button and the payment screen so both go through identical logic.
    /// Returns the created orders so the confirmation screen can show real
    /// details rather than a generic success message.
    /// </summary>
    Task<List<Order>> PlaceCartOrdersAsync(IEnumerable<CartItem> items, UserAccount user, Company? company, decimal deliveryFee);

    /// <summary>All orders (past and future) placed by a specific user.</summary>
    Task<List<Order>> GetOrdersForUserAsync(string userId);

    /// <summary>A user's own upcoming orders (DeliveryDate is today or later).</summary>
    Task<List<Order>> GetActiveOrdersForUserAsync(string userId);

    /// <summary>A user's own past orders (DeliveryDate before today).</summary>
    Task<List<Order>> GetOrderHistoryForUserAsync(string userId);

    /// <summary>Every upcoming order across every company — for the admin order-management screen.</summary>
    Task<List<Order>> GetAllActiveOrdersAsync();

    /// <summary>Every order regardless of user, company, or date — for admin reporting/analytics.</summary>
    Task<List<Order>> GetAllOrdersAsync();

    Task UpdateOrderStatusAsync(string orderId, string status);

    /// <summary>Customer's 1-5 star rating for a delivered order.</summary>
    Task UpdateOrderRatingAsync(string orderId, int rating);

    /// <summary>Optional written feedback alongside a star rating.</summary>
    Task UpdateOrderRatingFeedbackAsync(string orderId, string feedback);

    /// <summary>Customer flags a delivered order with an issue — generates a support ticket reference and sets the dispute to "Investigating".</summary>
    Task<string> ReportDisputeAsync(string orderId, string reason);

    /// <summary>Admin updates a dispute's resolution status.</summary>
    Task UpdateDisputeStatusAsync(string orderId, string status);
}
