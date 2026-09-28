using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YourKitchenCo.Models;

namespace YourKitchenCo.Services;

/// <summary>
/// In-memory discount codes, same "local data to get the feature working
/// end to end" stage as the other Mock*Service implementations — swap for a
/// Supabase-backed one behind the same interface when that migration happens.
/// </summary>
public class MockDiscountService : IDiscountService
{
    private readonly List<Discount> _discounts = new()
    {
        new Discount { Code = "WEEKEND20", Percentage = 20, Active = true, Expires = DateTime.Now.AddDays(6) },
        new Discount { Code = "WELCOME10", Percentage = 10, Active = true },
    };

    public Task<List<Discount>> GetDiscountsAsync() =>
        Task.FromResult(_discounts.OrderByDescending(d => d.Active).ToList());

    public Task<Discount> AddDiscountAsync(Discount discount)
    {
        _discounts.Insert(0, discount);
        return Task.FromResult(discount);
    }

    public Task UpdateDiscountAsync(Discount discount)
    {
        var index = _discounts.FindIndex(d => d.Id == discount.Id);
        if (index >= 0) _discounts[index] = discount;
        return Task.CompletedTask;
    }

    public Task DeleteDiscountAsync(string discountId)
    {
        _discounts.RemoveAll(d => d.Id == discountId);
        return Task.CompletedTask;
    }
}
