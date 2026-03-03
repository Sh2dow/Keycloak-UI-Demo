using backend.Dtos;
using MediatR;

namespace backend.Requests.Orders;

public sealed record UpdateOrderCommand(
    Guid Id,
    Guid UserId,
    decimal TotalAmount,
    string Status,
    string? DownloadUrl,
    string? ShippingAddress,
    string? TrackingNumber
) : IRequest<UpdateOrderResult>;

public sealed record UpdateOrderResult(bool NotFound, string? ValidationError, OrderViewDto? Order);
