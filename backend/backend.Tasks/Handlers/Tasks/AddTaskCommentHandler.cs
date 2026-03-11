using backend.Application.Results;
using backend.Application.Users;
using backend.Data;
using backend.Dtos;
using backend.Mappers;
using backend.Models;
using backend.Requests.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace backend.Handlers.Tasks;

public sealed class AddTaskCommentHandler : IRequestHandler<AddTaskCommentCommand, Result<TaskCommentDto>>
{
    private readonly AppDbContext _db;
    private readonly IEffectiveUserAccessor _effectiveUser;
    private readonly IUserDirectory _userDirectory;

    public AddTaskCommentHandler(AppDbContext db, IEffectiveUserAccessor effectiveUser, IUserDirectory userDirectory)
    {
        _db = db;
        _effectiveUser = effectiveUser;
        _userDirectory = userDirectory;
    }

    public async Task<Result<TaskCommentDto>> Handle(AddTaskCommentCommand req, CancellationToken ct)
    {
        var userId = await _effectiveUser.GetUserIdAsync(ct);
        var taskExists = await _db.Tasks
            .AnyAsync(x => x.Id == req.TaskId && x.UserId == userId, ct);
        if (!taskExists) return Result<TaskCommentDto>.NotFound("Task not found.");

        var comment = new TaskComment
        {
            TaskId = req.TaskId,
            AuthorId = userId,
            Content = req.Content.Trim()
        };

        _db.TaskComments.Add(comment);
        await _db.SaveChangesAsync(ct);

        var author = await _userDirectory.FindByIdAsync(userId, ct);
        var authorUsername = author?.Username ?? $"user-{userId.ToString("N")[..8]}";

        return Result<TaskCommentDto>.Success(comment.ToDto(authorUsername));
    }
}
