using MediatR;

namespace backend.Requests.Orders;

public sealed record DeleteOrderCommand(Guid Id, Guid UserId) : IRequest<bool>;
