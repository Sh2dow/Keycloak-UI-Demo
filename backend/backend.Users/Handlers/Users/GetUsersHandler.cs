using backend.Domain.Data;
using backend.Domain.Models;
using backend.Shared.Application.Users;
using backend.Users.Dtos;
using backend.Users.Mappers;
using backend.Users.Requests.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Users.Handlers.Users;

public sealed class GetUsersHandler : IRequestHandler<GetUsersQuery, IReadOnlyList<UserWithOrdersDto>>
{
    private readonly IUserDirectory _userDirectory;

    public GetUsersHandler(IUserDirectory userDirectory)
    {
        _userDirectory = userDirectory;
    }

    public async Task<IReadOnlyList<UserWithOrdersDto>> Handle(GetUsersQuery req, CancellationToken ct)
    {
        var users = await _userDirectory.ListAsync(ct);
        // In a real microservices architecture, orders would be fetched via HTTP call to orders-api
        // For now, return users without orders
        return users.Select(user => user.ToDto([])).ToList();
    }
}
