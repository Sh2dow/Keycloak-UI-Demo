using backend.Application.Abstractions;
using backend.Dtos;

namespace backend.Requests.Orders;

public sealed record GetOrdersQuery(int PageNumber = 1, int PageSize = 20) : IQuery<IReadOnlyList<OrderViewDto>>;
