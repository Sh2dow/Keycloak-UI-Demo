using MediatR;

namespace backend.Requests.Users;

public sealed record DeleteUserCommand(Guid Id) : IRequest<bool>;
