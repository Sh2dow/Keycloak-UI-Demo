using backend.Data;
using backend.Dtos;
using backend.Mappers;
using backend.Requests.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Users;

public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, CreateUserResult>
{
    private readonly AppDbContext _db;

    public CreateUserHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CreateUserResult> Handle(CreateUserCommand req, CancellationToken ct)
    {
        var subject = req.Subject.Trim();
        var exists = await _db.AppUsers.AnyAsync(x => x.Subject == subject, ct);
        if (exists) return new CreateUserResult(true, null);

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

        return new CreateUserResult(false, created.ToDto());
    }
}
