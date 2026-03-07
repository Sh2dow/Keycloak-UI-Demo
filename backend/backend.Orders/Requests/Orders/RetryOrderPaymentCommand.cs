using backend.Application.Abstractions;
using backend.Application.Results;
using backend.Dtos;

namespace backend.Requests.Orders;

public sealed record RetryOrderPaymentCommand(Guid OrderId) : ICommand<Result<OrderViewDto>>;
