using backend.Dtos;
using MediatR;

namespace backend.Requests.Users;

public sealed record CreateUserCommand(
    string Subject,
    string Username,
    string? Email
) : IRequest<CreateUserResult>;

public sealed record CreateUserResult(bool IsConflict, UserWithOrdersDto? User);
