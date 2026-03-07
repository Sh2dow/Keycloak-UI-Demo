using backend.Application.Abstractions;
using backend.Dtos;

namespace backend.Requests.Orders;

public sealed record GetOrderPaymentDetailsQuery(Guid OrderId) : IQuery<OrderPaymentDetailsDto?>;
