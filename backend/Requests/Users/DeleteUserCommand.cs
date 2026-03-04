using backend.Application.Abstractions;

namespace backend.Requests.Users;

public sealed record DeleteUserCommand(Guid Id) : ICommand<bool>;
