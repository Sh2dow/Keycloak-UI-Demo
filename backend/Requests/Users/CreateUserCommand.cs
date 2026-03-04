using backend.Application.Abstractions;
using backend.Dtos;

namespace backend.Requests.Users;

public sealed record CreateUserCommand(
    string Subject,
    string Username,
    string? Email
) : ICommand<CreateUserResult>;

public sealed record CreateUserResult(bool IsConflict, UserWithOrdersDto? User);
