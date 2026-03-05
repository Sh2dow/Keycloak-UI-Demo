using backend.Data;
using backend.Dtos;
using backend.Mappers;
using backend.Requests.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Users;

public sealed class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, UserWithOrdersDto?>
{
    private readonly AppDbContext _db;

    public GetUserByIdHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserWithOrdersDto?> Handle(GetUserByIdQuery req, CancellationToken ct)
    {
        var user = await _db.AppUsers
            .AsNoTracking()
            .Include(x => x.Orders)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct);

        return user?.ToDto();
    }
}
