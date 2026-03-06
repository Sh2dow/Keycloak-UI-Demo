using backend.Application.Results;
using backend.Data;
using backend.Dtos;
using backend.Mappers;
using backend.Requests.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Users;

public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, Result<UserWithOrdersDto>>
{
    private readonly AppDbContext _db;

    public CreateUserHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<UserWithOrdersDto>> Handle(CreateUserCommand req, CancellationToken ct)
    {
        var subject = req.Subject.Trim();
        var exists = await _db.AppUsers.AnyAsync(x => x.Subject == subject, ct);
        if (exists) return Result<UserWithOrdersDto>.Conflict("User with this subject already exists.");

        var user = req.ToEntity();
        user.Subject = subject;
        user.Username = req.Username.Trim();
        user.Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();

        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync(ct);

        var created = await _db.AppUsers
            .AsNoTracking()
            .Include(x => x.Orders)
            .FirstAsync(x => x.Id == user.Id, ct);

        return Result<UserWithOrdersDto>.Success(created.ToDto());
    }
}
