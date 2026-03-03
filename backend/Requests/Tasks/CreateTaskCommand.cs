using backend.Dtos;
using MediatR;

namespace backend.Requests.Tasks;

public sealed record CreateTaskCommand(
    Guid UserId,
    string Title,
    string? Description,
    string? Status,
    string? Priority
) : IRequest<TaskItemDto>;
