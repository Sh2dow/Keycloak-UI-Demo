using backend.Dtos;
using MediatR;

namespace backend.Requests.Orders;

public sealed record GetOrdersQuery(Guid UserId) : IRequest<IReadOnlyList<OrderViewDto>>;
