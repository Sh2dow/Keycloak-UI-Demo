using backend.Application.Abstractions;
using backend.Dtos;

namespace backend.Requests.Orders;

public sealed record GetOrderByIdQuery(Guid Id) : IQuery<OrderViewDto?>;
