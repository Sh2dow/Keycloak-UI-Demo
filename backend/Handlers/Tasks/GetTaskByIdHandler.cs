using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Requests.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Tasks;

public sealed class GetTaskByIdHandler : IRequestHandler<GetTaskByIdQuery, TaskItemDto?>
{
    private readonly AppDbContext _db;

    public GetTaskByIdHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<TaskItemDto?> Handle(GetTaskByIdQuery req, CancellationToken ct)
    {
        var task = await _db.Tasks
            .AsNoTrackingWithIdentityResolution()
            .Include(x => x.Comments)
            .ThenInclude(x => x.Author)
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.UserId == req.UserId, ct);

        return task == null ? null : MapTask(task);
    }

    private static TaskItemDto MapTask(TaskItem task) =>
        new(
            task.Id,
            task.UserId,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.CreatedAtUtc,
            task.UpdatedAtUtc,
            task.Comments
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new TaskCommentDto(
                    x.Id,
                    x.AuthorId,
                    x.Author.Username,
                    x.Content,
                    x.CreatedAtUtc
                ))
                .ToList()
        );
}
