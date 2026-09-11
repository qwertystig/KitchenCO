using System;

namespace YourKitchenCo.Models;

public class CompanyLocation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CompanyId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;          // e.g. "Building 2 - Sandton"
    public string Address { get; set; } = string.Empty;
    public string DeliveryInstructions { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Straight-line distance from the kitchen, in km — drives the delivery
    // fee tier (see DeliveryFeeCalculator). 0 until an admin sets it.
    public decimal DistanceKm { get; set; }
}
