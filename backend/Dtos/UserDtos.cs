namespace backend.Dtos;

public sealed record CreateUserRequest(string Subject, string Username, string? Email);

public sealed record UpdateUserRequest(string Username, string? Email);

public sealed record UserOrderSummaryDto(
    Guid Id,
    string OrderType,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAtUtc,
    string? DownloadUrl,
    string? ShippingAddress,
    string? TrackingNumber
);

public sealed record UserWithOrdersDto(
    Guid Id,
    string Subject,
    string Username,
    string? Email,
    DateTime CreatedAtUtc,
    IReadOnlyList<UserOrderSummaryDto> Orders
);
