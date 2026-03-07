using backend.Application.Abstractions;
using backend.Dtos;

namespace backend.Requests.Users;

public sealed record GetUserByIdQuery(Guid Id) : IQuery<UserWithOrdersDto?>;
