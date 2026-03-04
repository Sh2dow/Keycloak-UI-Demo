using backend.Application.Abstractions;
using backend.Dtos;

namespace backend.Requests.Orders;

public sealed record CreatePhysicalOrderCommand(
    decimal TotalAmount,
    string ShippingAddress,
    string? TrackingNumber
) : ICommand<OrderViewDto>;
