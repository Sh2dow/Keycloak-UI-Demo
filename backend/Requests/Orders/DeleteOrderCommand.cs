using backend.Application.Abstractions;

namespace backend.Requests.Orders;

public sealed record DeleteOrderCommand(Guid Id) : ICommand<bool>;
