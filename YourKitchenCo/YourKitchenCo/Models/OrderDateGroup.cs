using System;
using System.Collections.Generic;
using System.Linq;
using YourKitchenCo.ViewModel;

namespace YourKitchenCo.Models;

/// <summary>
/// One delivery date's worth of active orders — a CollectionView group
/// (IsGrouped="True" needs each group to be enumerable over its own rows,
/// the same way CategorySection works for the dashboard's menu).
/// </summary>
public class OrderDateGroup : List<ActiveOrderItem>
{
    public DateOnly Date { get; }

    public OrderDateGroup(DateOnly date, IEnumerable<ActiveOrderItem> items) : base(items)
    {
        Date = date;
    }

    /// <summary>"Today", "Tomorrow", or "Thursday, 02 Oct".</summary>
    public string DateLabel
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (Date == today) return "Today";
            if (Date == today.AddDays(1)) return "Tomorrow";
            return Date.ToString("dddd, dd MMM");
        }
    }

    /// <summary>"2 orders · R318.00" for the group header's right-hand side.</summary>
    public string Summary
    {
        get
        {
            var total = this.Sum(i => i.Order.TotalAmount);
            var noun = Count == 1 ? "order" : "orders";
            return $"{Count} {noun} · R{total:F2}";
        }
    }
}
