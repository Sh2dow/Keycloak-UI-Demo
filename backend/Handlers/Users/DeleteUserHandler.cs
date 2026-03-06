using backend.Application.Results;
using backend.Data;
using backend.Requests.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Users;

public sealed class DeleteUserHandler : IRequestHandler<DeleteUserCommand, Result<bool>>
{
    private readonly AppDbContext _db;

    public DeleteUserHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<bool>> Handle(DeleteUserCommand req, CancellationToken ct)
    {
        var affected = await _db.AppUsers
            .Where(x => x.Id == req.Id)
            .ExecuteDeleteAsync(ct);

        return affected > 0
            ? Result<bool>.Success(true)
            : Result<bool>.NotFound("User not found.");
    }
}
