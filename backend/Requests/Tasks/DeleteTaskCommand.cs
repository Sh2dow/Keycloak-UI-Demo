using MediatR;

namespace backend.Requests.Tasks;

public sealed record DeleteTaskCommand(Guid Id, Guid UserId) : IRequest<bool>;
