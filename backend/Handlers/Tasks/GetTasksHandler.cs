using backend.Application.Users;
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
    private readonly IEffectiveUserAccessor _effectiveUser;

    public GetTasksHandler(AppDbContext db, IEffectiveUserAccessor effectiveUser)
    {
        _db = db;
        _effectiveUser = effectiveUser;
    }

    public async Task<IReadOnlyList<TaskItemDto>> Handle(GetTasksQuery req, CancellationToken ct)
    {
        var userId = await _effectiveUser.GetUserIdAsync(ct);
        var pageNumber = Math.Max(req.PageNumber, 1);
        var pageSize = Math.Clamp(req.PageSize, 1, 100);

        var tasks = await _db.Tasks
            .AsNoTrackingWithIdentityResolution()
            .Where(x => x.UserId == userId)
            .Include(x => x.Comments)
            .ThenInclude(x => x.Author)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
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
