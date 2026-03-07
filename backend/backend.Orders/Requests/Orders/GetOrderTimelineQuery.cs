using backend.Application.Abstractions;
using backend.Dtos;

namespace backend.Requests.Orders;

public sealed record GetOrderTimelineQuery(Guid OrderId) : IQuery<IReadOnlyList<OrderTimelineItemDto>?>;
