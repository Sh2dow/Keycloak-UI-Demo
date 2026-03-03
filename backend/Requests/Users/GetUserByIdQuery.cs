using backend.Dtos;
using MediatR;

namespace backend.Requests.Users;

public sealed record GetUserByIdQuery(Guid Id) : IRequest<UserWithOrdersDto?>;
