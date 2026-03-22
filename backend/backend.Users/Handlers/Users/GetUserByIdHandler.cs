using backend.Domain.Data;
using backend.Shared.Application.Users;
using backend.Users.Dtos;
using backend.Users.Mappers;
using backend.Users.Requests.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Users.Handlers.Users;

public sealed class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, UserWithOrdersDto?>
{
    private readonly IUserDirectory _userDirectory;
    private readonly OrdersDbContext _ordersDb;

    public GetUserByIdHandler(IUserDirectory userDirectory, OrdersDbContext ordersDb)
    {
        _userDirectory = userDirectory;
        _ordersDb = ordersDb;
    }

    public async Task<UserWithOrdersDto?> Handle(GetUserByIdQuery req, CancellationToken ct)
    {
        var user = await _userDirectory.FindByIdAsync(req.Id, ct);
        if (user == null)
        {
            return null;
        }

        var orders = await _ordersDb.Orders
            .AsNoTracking()
            .Where(x => x.UserId == req.Id)
            .ToListAsync(ct);

        return user.ToDto(orders);
    }
}
