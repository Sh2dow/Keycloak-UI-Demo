namespace backend.Dtos;

public sealed record CreateDigitalOrderRequest(decimal TotalAmount, string DownloadUrl);

public sealed record CreatePhysicalOrderRequest(decimal TotalAmount, string ShippingAddress, string? TrackingNumber);

public sealed record CreateOrderRequest(
    string OrderType,
    decimal TotalAmount,
    string? DownloadUrl,
    string? ShippingAddress,
    string? TrackingNumber
);

public sealed record UpdateOrderRequest(
    decimal TotalAmount,
    string Status,
    string? DownloadUrl,
    string? ShippingAddress,
    string? TrackingNumber
);
