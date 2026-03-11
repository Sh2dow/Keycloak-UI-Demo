using backend.Data;
using backend.Dtos;
using backend.Mappers;
using backend.Requests.Users;
using backend.Application.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Users;

public sealed class GetUsersHandler : IRequestHandler<GetUsersQuery, IReadOnlyList<UserWithOrdersDto>>
{
    private readonly IUserDirectory _userDirectory;
    private readonly AppDbContext _appDb;

    public GetUsersHandler(IUserDirectory userDirectory, AppDbContext appDb)
    {
        _userDirectory = userDirectory;
        _appDb = appDb;
    }

    public async Task<IReadOnlyList<UserWithOrdersDto>> Handle(GetUsersQuery req, CancellationToken ct)
    {
        var users = await _userDirectory.ListAsync(ct);
        var userIds = users.Select(x => x.Id).ToArray();
        var orders = await _appDb.Orders
            .AsNoTracking()
            .Where(x => userIds.Contains(x.UserId))
            .ToListAsync(ct);
        var ordersByUserId = orders.GroupBy(x => x.UserId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<backend.Models.Order>)x.ToList());

        return users
            .Select(user => user.ToDto(ordersByUserId.GetValueOrDefault(user.Id, [])))
            .ToList();
    }
}
