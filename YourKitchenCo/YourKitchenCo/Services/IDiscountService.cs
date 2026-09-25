using System.Collections.Generic;
using System.Threading.Tasks;
using YourKitchenCo.Models;

namespace YourKitchenCo.Services;

public interface IDiscountService
{
    Task<List<Discount>> GetDiscountsAsync();
    Task<Discount> AddDiscountAsync(Discount discount);
    Task UpdateDiscountAsync(Discount discount);
    Task DeleteDiscountAsync(string discountId);
}
