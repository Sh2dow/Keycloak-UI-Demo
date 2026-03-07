using backend.Application.Abstractions;
using backend.Application.Results;

namespace backend.Requests.Orders;

public sealed record DeleteOrderCommand(Guid Id) : ICommand<Result<bool>>;
