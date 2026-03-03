using backend.Data;
using backend.Requests.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Users;

public sealed class DeleteUserHandler : IRequestHandler<DeleteUserCommand, bool>
{
    private readonly AppDbContext _db;

    public DeleteUserHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(DeleteUserCommand req, CancellationToken ct)
    {
        var affected = await _db.AppUsers
            .Where(x => x.Id == req.Id)
            .ExecuteDeleteAsync(ct);

        return affected > 0;
    }
}
