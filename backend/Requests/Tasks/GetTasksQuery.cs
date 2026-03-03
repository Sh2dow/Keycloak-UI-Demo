using backend.Dtos;
using MediatR;

namespace backend.Requests.Tasks;

public sealed record GetTasksQuery(Guid UserId) : IRequest<IReadOnlyList<TaskItemDto>>;
