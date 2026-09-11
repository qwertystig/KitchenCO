using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YourKitchenCo.Models;

namespace YourKitchenCo.Services;

public interface IProductService
{
    Task<List<Product>> GetProductsAsync();
    Task<List<Product>> GetProductsAsync(string category);

    // Admin CRUD for the static (always-available) menu. These mutate the
    // same underlying list GetProductsAsync reads from, so changes show up
    // on the customer dashboard immediately — not just in the admin's own
    // local copy.
    Task<Product> AddStaticProductAsync(Product product);
    Task UpdateStaticProductAsync(Product product);
    Task DeleteStaticProductAsync(int productId);
}
