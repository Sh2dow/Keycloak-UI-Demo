using backend.Dtos;
using MediatR;

namespace backend.Requests.Users;

public sealed record GetUsersQuery() : IRequest<IReadOnlyList<UserWithOrdersDto>>;
