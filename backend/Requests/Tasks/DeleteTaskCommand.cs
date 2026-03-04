using backend.Application.Abstractions;

namespace backend.Requests.Tasks;

public sealed record DeleteTaskCommand(Guid Id) : ICommand<bool>;
