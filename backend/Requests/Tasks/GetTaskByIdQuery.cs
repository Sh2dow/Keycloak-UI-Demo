using backend.Dtos;
using MediatR;

namespace backend.Requests.Tasks;

public sealed record GetTaskByIdQuery(Guid Id, Guid UserId) : IRequest<TaskItemDto?>;
