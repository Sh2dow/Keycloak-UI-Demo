using backend.Data;
using backend.Dtos;
using backend.Mappers;
using backend.Requests.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Users;

public sealed class GetUsersHandler : IRequestHandler<GetUsersQuery, IReadOnlyList<UserWithOrdersDto>>
{
    private readonly AppDbContext _db;

    public GetUsersHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserWithOrdersDto>> Handle(GetUsersQuery req, CancellationToken ct)
    {
        var users = await _db.AppUsers
            .AsNoTracking()
            .Include(x => x.Orders)
            .OrderBy(x => x.Username)
            .ToListAsync(ct);

        return users.Select(UserMapper.ToDto).ToList();
    }
}
