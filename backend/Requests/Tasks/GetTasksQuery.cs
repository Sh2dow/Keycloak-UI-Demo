using backend.Application.Abstractions;
using backend.Dtos;

namespace backend.Requests.Tasks;

public sealed record GetTasksQuery(int PageNumber = 1, int PageSize = 20) : IQuery<IReadOnlyList<TaskItemDto>>;
