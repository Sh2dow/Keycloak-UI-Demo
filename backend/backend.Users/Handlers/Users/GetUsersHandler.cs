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
    private readonly OrdersDbContext _ordersDb;

    public GetUsersHandler(IUserDirectory userDirectory, OrdersDbContext ordersDb)
    {
        _userDirectory = userDirectory;
        _ordersDb = ordersDb;
    }

    public async Task<IReadOnlyList<UserWithOrdersDto>> Handle(GetUsersQuery req, CancellationToken ct)
    {
        var users = await _userDirectory.ListAsync(ct);
        var userIds = users.Select(x => x.Id).ToArray();
        var orders = await _ordersDb.Orders
            .AsNoTracking()
            .Where(x => userIds.Contains(x.UserId))
            .ToListAsync(ct);
        var ordersByUserId = orders.GroupBy(x => x.UserId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<Order>)x.ToList());

        return users
            .Select(user => user.ToDto(ordersByUserId.GetValueOrDefault(user.Id, [])))
            .ToList();
    }
}
