using backend.Application.Users;
using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Requests.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Tasks;

public sealed class AddTaskCommentHandler : IRequestHandler<AddTaskCommentCommand, TaskCommentDto?>
{
    private readonly AppDbContext _db;
    private readonly IEffectiveUserAccessor _effectiveUser;

    public AddTaskCommentHandler(AppDbContext db, IEffectiveUserAccessor effectiveUser)
    {
        _db = db;
        _effectiveUser = effectiveUser;
    }

    public async Task<TaskCommentDto?> Handle(AddTaskCommentCommand req, CancellationToken ct)
    {
        var userId = await _effectiveUser.GetUserIdAsync(ct);
        var taskExists = await _db.Tasks
            .AnyAsync(x => x.Id == req.TaskId && x.UserId == userId, ct);
        if (!taskExists) return null;

        var comment = new TaskComment
        {
            TaskId = req.TaskId,
            AuthorId = userId,
            Content = req.Content.Trim()
        };

        _db.TaskComments.Add(comment);
        await _db.SaveChangesAsync(ct);

        var created = await _db.TaskComments
            .AsNoTracking()
            .Include(x => x.Author)
            .FirstAsync(x => x.Id == comment.Id, ct);

        return new TaskCommentDto(
            created.Id,
            created.AuthorId,
            created.Author.Username,
            created.Content,
            created.CreatedAtUtc
        );
    }
}
