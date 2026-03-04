using backend.Application.Abstractions;
using backend.Dtos;

namespace backend.Requests.Tasks;

public sealed record GetTaskByIdQuery(Guid Id) : IQuery<TaskItemDto?>;
