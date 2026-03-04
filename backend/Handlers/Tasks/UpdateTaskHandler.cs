using backend.Application.Users;
using backend.Data;
using backend.Dtos;
using backend.Requests.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Tasks;

public sealed class UpdateTaskHandler : IRequestHandler<UpdateTaskCommand, TaskItemDto?>
{
    private readonly AppDbContext _db;
    private readonly IEffectiveUserAccessor _effectiveUser;

    public UpdateTaskHandler(AppDbContext db, IEffectiveUserAccessor effectiveUser)
    {
        _db = db;
        _effectiveUser = effectiveUser;
    }

    public async Task<TaskItemDto?> Handle(UpdateTaskCommand req, CancellationToken ct)
    {
        var userId = await _effectiveUser.GetUserIdAsync(ct);
        var task = await _db.Tasks
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.UserId == userId, ct);
        if (task == null) return null;

        if (!string.IsNullOrWhiteSpace(req.Title))
        {
            task.Title = req.Title.Trim();
        }

        task.Description = req.Description?.Trim();
        task.Status = NormalizeStatus(req.Status);
        task.Priority = NormalizePriority(req.Priority);
        task.UpdatedAtUtc = DateTime.UtcNow;

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
