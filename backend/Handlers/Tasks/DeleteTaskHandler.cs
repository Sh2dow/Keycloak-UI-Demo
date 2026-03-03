using backend.Data;
using backend.Requests.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Tasks;

public sealed class DeleteTaskHandler : IRequestHandler<DeleteTaskCommand, bool>
{
    private readonly AppDbContext _db;

    public DeleteTaskHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(DeleteTaskCommand req, CancellationToken ct)
    {
        var affected = await _db.Tasks
            .Where(x => x.Id == req.Id && x.UserId == req.UserId)
            .ExecuteDeleteAsync(ct);

        return affected > 0;
    }
}
