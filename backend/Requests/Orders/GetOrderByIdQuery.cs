using backend.Dtos;
using MediatR;

namespace backend.Requests.Orders;

public sealed record GetOrderByIdQuery(Guid Id, Guid UserId) : IRequest<OrderViewDto?>;
