namespace backend.Models;

public class PhysicalOrder : Order
{
    public string ShippingAddress { get; set; } = string.Empty;

    public string? TrackingNumber { get; set; }
}
