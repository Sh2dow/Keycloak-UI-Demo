using backend.Application.Results;
using backend.Application.Users;
using backend.Data;
using backend.Requests.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Tasks;

public sealed class DeleteTaskHandler : IRequestHandler<DeleteTaskCommand, Result<bool>>
{
    private readonly AppDbContext _db;
    private readonly IEffectiveUserAccessor _effectiveUser;

    public DeleteTaskHandler(AppDbContext db, IEffectiveUserAccessor effectiveUser)
    {
        _db = db;
        _effectiveUser = effectiveUser;
    }

    public async Task<Result<bool>> Handle(DeleteTaskCommand req, CancellationToken ct)
    {
        var userId = await _effectiveUser.GetUserIdAsync(ct);
        var affected = await _db.Tasks
            .Where(x => x.Id == req.Id && x.UserId == userId)
            .ExecuteDeleteAsync(ct);

        return affected > 0
            ? Result<bool>.Success(true)
            : Result<bool>.NotFound("Task not found.");
    }
}
