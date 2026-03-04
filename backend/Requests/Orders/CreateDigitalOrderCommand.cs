using backend.Application.Abstractions;
using backend.Dtos;

namespace backend.Requests.Orders;

public sealed record CreateDigitalOrderCommand(
    decimal TotalAmount,
    string DownloadUrl
) : ICommand<OrderViewDto>;
