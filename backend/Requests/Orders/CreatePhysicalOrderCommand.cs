using backend.Dtos;
using MediatR;

namespace backend.Requests.Orders;

public sealed record CreatePhysicalOrderCommand(
    Guid UserId,
    decimal TotalAmount,
    string ShippingAddress,
    string? TrackingNumber
) : IRequest<OrderViewDto>;
