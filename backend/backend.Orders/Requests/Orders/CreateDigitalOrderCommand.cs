using backend.Application.Abstractions;
using backend.Application.Results;
using backend.Dtos;

namespace backend.Requests.Orders;

public sealed record CreateDigitalOrderCommand(
    decimal TotalAmount,
    string DownloadUrl
) : ICommand<Result<OrderViewDto>>;
