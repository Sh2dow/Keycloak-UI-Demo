using backend.Dtos;
using backend.Models;
using backend.Requests.Tasks;
using Riok.Mapperly.Abstractions;

namespace backend.Mappers;

[Mapper]
public static partial class TaskMapper
{
    [MapperIgnoreSource(nameof(TaskComment.TaskId))]
    [MapperIgnoreSource(nameof(TaskComment.Task))]
    [MapProperty("Author.Username", nameof(TaskCommentDto.AuthorUsername))]
    public static partial TaskCommentDto ToDto(this TaskComment comment);

    [MapperIgnoreSource(nameof(TaskItem.User))]
    public static partial TaskItemDto ToDto(this TaskItem task);

    [MapperIgnoreTarget(nameof(TaskItem.Id))]
    [MapperIgnoreTarget(nameof(TaskItem.UserId))]
    [MapperIgnoreTarget(nameof(TaskItem.User))]
    [MapperIgnoreTarget(nameof(TaskItem.CreatedAtUtc))]
    [MapperIgnoreTarget(nameof(TaskItem.UpdatedAtUtc))]
    [MapperIgnoreTarget(nameof(TaskItem.Comments))]
    public static partial TaskItem ToEntity(this CreateTaskCommand command);

    public static partial IQueryable<TaskCommentDto> ProjectToTaskCommentDto(this IQueryable<TaskComment> source);

    public static partial IQueryable<TaskItemDto> ProjectToTaskItemDto(this IQueryable<TaskItem> source);
}
