using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YourKitchenCo.Models;

namespace YourKitchenCo.Services;

/// <summary>
/// In-memory stand-in for the orders/order_items tables. Registered as a
/// singleton so orders placed during the session show up in Active Orders /
/// Order History / the admin order screen immediately. Swap for a
/// Supabase-backed implementation later — the interface doesn't need to change.
/// </summary>
public class MockOrderService : IOrderService
{
    private readonly List<Order> _orders;
    private int _nextOrderNumber = 2001;

    public MockOrderService()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);

        _orders = new List<Order>
        {
            // --- Past orders (order history) ---
            new() { OrderNumber = "1001", UserId = "seed-john", CustomerName = "John Doe", Status = "Delivered",
                    ItemName = "Traditional Beef Bobotie", CompanyId = "company-ecogra", LocationId = "loc-ecogra-rosebank",
                    MenuType = MenuType.Cycle, DeliveryDate = today.AddDays(-9), OrderDate = DateTime.Now.AddDays(-11), TotalAmount = 95.00m },
            new() { OrderNumber = "1002", UserId = "seed-sarah", CustomerName = "Sarah Smith", Status = "Delivered",
                    ItemName = "Fresh Caesar Salad", CompanyId = "company-ecogra", LocationId = "loc-ecogra-rosebank",
                    MenuType = MenuType.Static, DeliveryDate = today.AddDays(-4), OrderDate = DateTime.Now.AddDays(-6), TotalAmount = 65.00m },
            new() { OrderNumber = "1003", UserId = "seed-priya", CustomerName = "Priya Naidoo", Status = "Delivered",
                    ItemName = "Grilled Salmon Filet", CompanyId = "company-tata", LocationId = "loc-tata-illovo",
                    MenuType = MenuType.Static, DeliveryDate = today.AddDays(-2), OrderDate = DateTime.Now.AddDays(-4), TotalAmount = 165.00m },

            // --- Upcoming / active orders ---
            new() { OrderNumber = "1004", UserId = "seed-john", CustomerName = "John Doe", Status = "Received",
                    ItemName = "Flame-grilled Boerewors with Chakalaka & Pap", CompanyId = "company-ecogra", LocationId = "loc-ecogra-rosebank",
                    MenuType = MenuType.Cycle, DeliveryDate = today.AddDays(2), OrderDate = DateTime.Now, TotalAmount = 95.00m },
            new() { OrderNumber = "1005", UserId = "seed-sarah", CustomerName = "Sarah Smith", Status = "Received",
                    ItemName = "Gourmet Beef Burger", CompanyId = "company-ecogra", LocationId = "loc-ecogra-rosebank",
                    MenuType = MenuType.Static, DeliveryDate = today.AddDays(2), OrderDate = DateTime.Now, TotalAmount = 95.00m },
            new() { OrderNumber = "1006", UserId = "seed-priya", CustomerName = "Priya Naidoo", Status = "Preparing",
                    ItemName = "Weekly Special: Spicy Taco Bowl", CompanyId = "company-tata", LocationId = "loc-tata-illovo",
                    MenuType = MenuType.Cycle, DeliveryDate = today.AddDays(1), OrderDate = DateTime.Now.AddDays(-1), TotalAmount = 120.00m },
            new() { OrderNumber = "1007", UserId = "seed-thabo", CustomerName = "Thabo Nkosi", Status = "Received",
                    ItemName = "Chicken Schnitzel", CompanyId = "company-rcl", LocationId = "loc-rcl-bedfordview",
                    MenuType = MenuType.Static, DeliveryDate = today.AddDays(3), OrderDate = DateTime.Now, TotalAmount = 110.00m },
            new() { OrderNumber = "1008", UserId = "seed-thabo", CustomerName = "Thabo Nkosi", Status = "Received",
                    ItemName = "Margherita Pizza", CompanyId = "company-rcl", LocationId = "loc-rcl-bedfordview",
                    MenuType = MenuType.Static, DeliveryDate = today.AddDays(5), OrderDate = DateTime.Now, TotalAmount = 85.00m },
        };
    }

    public Task<Order> PlaceOrderAsync(Order order)
    {
        if (string.IsNullOrWhiteSpace(order.Id))
            order.Id = Guid.NewGuid().ToString();

        if (string.IsNullOrWhiteSpace(order.OrderNumber))
            order.OrderNumber = (_nextOrderNumber++).ToString();

        if (string.IsNullOrWhiteSpace(order.Status))
            order.Status = "Received";

        _orders.Add(order);
        return Task.FromResult(order);
    }

    public async Task<List<Order>> PlaceCartOrdersAsync(IEnumerable<CartItem> items, UserAccount user, Company? company, decimal deliveryFee)
    {
        // One order per cart line for now (each item already carries its own
        // delivery date + menu type). Once there's a real order_items table
        // these will collapse into a single order per delivery date with
        // multiple line items instead. The delivery fee is a single flat
        // charge per checkout (not per item), so it's recorded on the first
        // order only — otherwise summing orders later would multiply it.
        var isFirstOrder = true;
        var created = new List<Order>();

        foreach (var item in items)
        {
            var grossAmount = item.FinalPrice * item.Quantity;
            var subsidyAmount = company is { MealSubsidyAmount: > 0 }
                ? Math.Min(company.MealSubsidyAmount, item.FinalPrice) * item.Quantity
                : 0m;

            var afterSubsidy = Math.Max(0, grossAmount - subsidyAmount);

            // Company discount (percentage or flat ZAR), applied after the
            // subsidy — separate lever from the subsidy, a company can have
            // either, both, or neither. Never lets a line go negative.
            var discountAmount = company?.DiscountType switch
            {
                DiscountType.Percentage => Math.Round(afterSubsidy * (company.DiscountValue / 100m), 2),
                DiscountType.FixedZar => Math.Min(company.DiscountValue, afterSubsidy),
                _ => 0m
            };

            var order = new Order
            {
                UserId = user.Id,
                CustomerName = user.FullName,
                CompanyId = user.CompanyId,
                LocationId = user.LocationId,
                DeliveryFloor = user.DeliveryFloor,
                ItemName = item.Quantity > 1 ? $"{item.Quantity}x {item.Product.Name}" : item.Product.Name,
                Category = item.Product.Category,
                SummaryText = item.SpecialRequests,
                AllergyNotes = item.AllergyNotes,
                TotalAmount = Math.Max(0, afterSubsidy - discountAmount) + (isFirstOrder ? deliveryFee : 0m),
                SubsidyAmount = subsidyAmount,
                DiscountAmount = discountAmount,
                DeliveryFee = isFirstOrder ? deliveryFee : 0m,
                DeliveryDate = item.DeliveryDate,
                MenuType = item.MenuType,
                OrderDate = DateTime.Now,
                Status = "Received",
                TaxInvoiceNumber = GenerateTaxInvoiceNumber()
            };

            await PlaceOrderAsync(order);
            created.Add(order);
            isFirstOrder = false;
        }

        return created;
    }

    private int _nextInvoiceSequence = 1;

    /// <summary>"INV-KC-2609-0001" — INV-KC-YYMM-#### — matching the reference app's tax invoice number format.</summary>
    private string GenerateTaxInvoiceNumber()
    {
        var yearMonth = DateTime.Now.ToString("yyMM");
        return $"INV-KC-{yearMonth}-{_nextInvoiceSequence++:D4}";
    }

    public Task<List<Order>> GetOrdersForUserAsync(string userId) =>
        Task.FromResult(_orders.Where(o => o.UserId == userId)
                                .OrderByDescending(o => o.DeliveryDate)
                                .ToList());

    public Task<List<Order>> GetActiveOrdersForUserAsync(string userId)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        return Task.FromResult(_orders
            .Where(o => o.UserId == userId && o.DeliveryDate >= today)
            .OrderBy(o => o.DeliveryDate)
            .ToList());
    }

    public Task<List<Order>> GetOrderHistoryForUserAsync(string userId)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        return Task.FromResult(_orders
            .Where(o => o.UserId == userId && o.DeliveryDate < today)
            .OrderByDescending(o => o.DeliveryDate)
            .ToList());
    }

    public Task<List<Order>> GetAllActiveOrdersAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        return Task.FromResult(_orders
            .Where(o => o.DeliveryDate >= today)
            .OrderBy(o => o.CompanyId).ThenBy(o => o.LocationId).ThenBy(o => o.DeliveryDate)
            .ToList());
    }

    public Task<List<Order>> GetAllOrdersAsync() =>
        Task.FromResult(_orders.OrderByDescending(o => o.DeliveryDate).ToList());

    public Task UpdateOrderStatusAsync(string orderId, string status)
    {
        var order = _orders.FirstOrDefault(o => o.Id == orderId);
        if (order is not null)
            order.Status = status;
        return Task.CompletedTask;
    }

    public Task UpdateOrderRatingAsync(string orderId, int rating)
    {
        var order = _orders.FirstOrDefault(o => o.Id == orderId);
        if (order is not null)
            order.Rating = Math.Clamp(rating, 0, 5);
        return Task.CompletedTask;
    }

    public Task UpdateOrderRatingFeedbackAsync(string orderId, string feedback)
    {
        var order = _orders.FirstOrDefault(o => o.Id == orderId);
        if (order is not null)
            order.RatingFeedback = feedback ?? string.Empty;
        return Task.CompletedTask;
    }

    private int _nextTicketNumber = 88001;

    public Task<string> ReportDisputeAsync(string orderId, string reason)
    {
        var order = _orders.FirstOrDefault(o => o.Id == orderId);
        if (order is null) return Task.FromResult(string.Empty);

        var ticketRef = $"TCK-{_nextTicketNumber++}";
        order.DisputeReason = reason;
        order.DisputeTicketRef = ticketRef;
        order.DisputeReportedAt = DateTime.Now;
        order.DisputeStatus = "Investigating";

        return Task.FromResult(ticketRef);
    }

    public Task UpdateDisputeStatusAsync(string orderId, string status)
    {
        var order = _orders.FirstOrDefault(o => o.Id == orderId);
        if (order is not null)
            order.DisputeStatus = status;
        return Task.CompletedTask;
    }
}
