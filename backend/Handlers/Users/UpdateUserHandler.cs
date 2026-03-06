using backend.Application.Results;
using backend.Data;
using backend.Dtos;
using backend.Mappers;
using backend.Requests.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Users;

public sealed class UpdateUserHandler : IRequestHandler<UpdateUserCommand, Result<UserWithOrdersDto>>
{
    private readonly AppDbContext _db;

    public UpdateUserHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<UserWithOrdersDto>> Handle(UpdateUserCommand req, CancellationToken ct)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (user == null) return Result<UserWithOrdersDto>.NotFound("User not found.");

        user.Username = req.Username.Trim();
        user.Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();
        await _db.SaveChangesAsync(ct);

        var updated = await _db.AppUsers
            .AsNoTracking()
            .Include(x => x.Orders)
            .FirstAsync(x => x.Id == req.Id, ct);

        return Result<UserWithOrdersDto>.Success(updated.ToDto());
    }
}
