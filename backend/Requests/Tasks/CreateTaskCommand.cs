using backend.Application.Abstractions;
using backend.Application.Results;
using backend.Dtos;

namespace backend.Requests.Tasks;

public sealed record CreateTaskCommand(
    string Title,
    string? Description,
    string? Status,
    string? Priority
) : ICommand<Result<TaskItemDto>>;
