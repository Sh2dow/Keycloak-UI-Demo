using backend.Application.Users;
using backend.Data;
using backend.Dtos;
using backend.Requests.Tasks;
using MediatR;

namespace backend.Handlers.Tasks;

public sealed class CreateTaskHandler : IRequestHandler<CreateTaskCommand, TaskItemDto>
{
    private readonly AppDbContext _db;
    private readonly IEffectiveUserAccessor _effectiveUser;

    public CreateTaskHandler(AppDbContext db, IEffectiveUserAccessor effectiveUser)
    {
        _db = db;
        _effectiveUser = effectiveUser;
    }

    public async Task<TaskItemDto> Handle(CreateTaskCommand req, CancellationToken ct)
    {
        var userId = await _effectiveUser.GetUserIdAsync(ct);
        var task = new Models.TaskItem
        {
            UserId = userId,
            Title = req.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
            Status = NormalizeStatus(req.Status),
            Priority = NormalizePriority(req.Priority)
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(ct);

        return new TaskItemDto(
            task.Id,
            task.UserId,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.CreatedAtUtc,
            task.UpdatedAtUtc,
            []
        );
    }

    private static string NormalizeStatus(string? status)
    {
        var normalized = status?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "todo" => "todo",
            "in-progress" => "in-progress",
            "done" => "done",
            _ => "todo"
        };
    }

    private static string NormalizePriority(string? priority)
    {
        var normalized = priority?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "low" => "low",
            "medium" => "medium",
            "high" => "high",
            _ => "medium"
        };
    }
}
