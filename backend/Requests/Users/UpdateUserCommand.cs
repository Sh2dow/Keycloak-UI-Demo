using backend.Dtos;
using MediatR;

namespace backend.Requests.Users;

public sealed record UpdateUserCommand(
    Guid Id,
    string Username,
    string? Email
) : IRequest<UserWithOrdersDto?>;
