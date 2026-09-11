using YourKitchenCo.Models;

namespace YourKitchenCo.ViewModel;

/// <summary>Order plus resolved company/location names, for the admin active-orders grid.</summary>
public class AdminOrderRow
{
    public Order Order { get; set; } = new();
    public string CompanyName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;

    public string DeliveryDateLabel => Order.DeliveryDate.ToString("dddd, dd MMM");
}
