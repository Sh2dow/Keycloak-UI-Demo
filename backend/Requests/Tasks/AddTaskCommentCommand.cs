using backend.Dtos;
using MediatR;

namespace backend.Requests.Tasks;

public sealed record AddTaskCommentCommand(Guid TaskId, Guid UserId, string Content) : IRequest<TaskCommentDto?>;
