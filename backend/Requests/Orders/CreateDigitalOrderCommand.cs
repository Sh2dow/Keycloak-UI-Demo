using backend.Dtos;
using MediatR;

namespace backend.Requests.Orders;

public sealed record CreateDigitalOrderCommand(
    Guid UserId,
    decimal TotalAmount,
    string DownloadUrl
) : IRequest<OrderViewDto>;
