using backend.Application.Abstractions;
using backend.Application.Results;
using backend.Dtos;

namespace backend.Requests.Tasks;

public sealed record AddTaskCommentCommand(Guid TaskId, string Content) : ICommand<Result<TaskCommentDto>>;
