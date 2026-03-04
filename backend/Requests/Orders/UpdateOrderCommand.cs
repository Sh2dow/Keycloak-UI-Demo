using backend.Application.Abstractions;
using backend.Dtos;

namespace backend.Requests.Orders;

public sealed record UpdateOrderCommand(
    Guid Id,
    decimal TotalAmount,
    string Status,
    string? DownloadUrl,
    string? ShippingAddress,
    string? TrackingNumber
) : ICommand<UpdateOrderResult>;

public sealed record UpdateOrderResult(bool NotFound, string? ValidationError, OrderViewDto? Order);
