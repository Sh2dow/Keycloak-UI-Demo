using backend.Application.Abstractions;
using backend.Application.Results;

namespace backend.Requests.Users;

public sealed record DeleteUserCommand(Guid Id) : ICommand<Result<bool>>;
