using backend.Data;
using backend.Dtos;
using backend.Mappers;
using backend.Requests.Users;
using backend.Application.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Users;

public sealed class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, UserWithOrdersDto?>
{
    private readonly IUserDirectory _userDirectory;
    private readonly AppDbContext _appDb;

    public GetUserByIdHandler(IUserDirectory userDirectory, AppDbContext appDb)
    {
        _userDirectory = userDirectory;
        _appDb = appDb;
    }

    public async Task<UserWithOrdersDto?> Handle(GetUserByIdQuery req, CancellationToken ct)
    {
        var user = await _userDirectory.FindByIdAsync(req.Id, ct);
        if (user == null)
        {
            return null;
        }

        var orders = await _appDb.Orders
            .AsNoTracking()
            .Where(x => x.UserId == req.Id)
            .ToListAsync(ct);

        return user.ToDto(orders);
    }
}
