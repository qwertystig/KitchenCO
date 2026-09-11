using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using YourKitchenCo.Models;

namespace YourKitchenCo.Services;

public interface ICartService
{
    ObservableCollection<CartItem> Items { get; }
    void AddItem(CartItem item);
    void RemoveItem(CartItem item);
    decimal GetTotal();
    void ClearCart();
}

public class CartService : ICartService
{
    public ObservableCollection<CartItem> Items { get; } = new();

    public void AddItem(CartItem item)
    {
        Items.Add(item);
    }

    public void RemoveItem(CartItem item)
    {
        Items.Remove(item);
    }

    public decimal GetTotal()
    {
        decimal total = 0;
        foreach (var item in Items)
        {
            total += item.FinalPrice;
        }
        return total;
    }

    public void ClearCart()
    {
        Items.Clear();
    }
}