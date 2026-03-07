using backend.Application.Abstractions;
using backend.Dtos;

namespace backend.Requests.Orders;

public sealed record GetOrderWorkflowQuery(Guid OrderId) : IQuery<OrderWorkflowDto?>;
