using backend.Application.Abstractions;
using backend.Application.Results;
using backend.Dtos;

namespace backend.Requests.Orders;

public sealed record UpdateOrderCommand(
    Guid Id,
    decimal TotalAmount,
    string Status,
    string? DownloadUrl,
    string? ShippingAddress,
    string? TrackingNumber
) : ICommand<Result<OrderViewDto>>;
