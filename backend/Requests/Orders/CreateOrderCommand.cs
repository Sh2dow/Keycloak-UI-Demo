using backend.Application.Abstractions;
using backend.Application.Results;
using backend.Dtos;

namespace backend.Requests.Orders;

public sealed record CreateOrderCommand(
    string OrderType,
    decimal TotalAmount,
    string? DownloadUrl,
    string? ShippingAddress,
    string? TrackingNumber
) : ICommand<Result<OrderViewDto>>;
