using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Requests.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Tasks;

public sealed class GetTasksHandler : IRequestHandler<GetTasksQuery, IReadOnlyList<TaskItemDto>>
{
    private readonly AppDbContext _db;

    public GetTasksHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TaskItemDto>> Handle(GetTasksQuery req, CancellationToken ct)
    {
        var tasks = await _db.Tasks
            .AsNoTrackingWithIdentityResolution()
            .Where(x => x.UserId == req.UserId)
            .Include(x => x.Comments)
            .ThenInclude(x => x.Author)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);

        return tasks.Select(MapTask).ToList();
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
