namespace backend.Dtos;

public sealed record OrderViewDto(
    Guid Id,
    string OrderType,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAtUtc,
    string? DownloadUrl,
    string? ShippingAddress,
    string? TrackingNumber
);
