using backend.Application.Abstractions;
using backend.Dtos;

namespace backend.Requests.Tasks;

public sealed record UpdateTaskCommand(
    Guid Id,
    string? Title,
    string? Description,
    string? Status,
    string? Priority
) : ICommand<TaskItemDto?>;
