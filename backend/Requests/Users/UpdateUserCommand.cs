using backend.Application.Abstractions;
using backend.Dtos;

namespace backend.Requests.Users;

public sealed record UpdateUserCommand(
    Guid Id,
    string Username,
    string? Email
) : ICommand<UserWithOrdersDto?>;
