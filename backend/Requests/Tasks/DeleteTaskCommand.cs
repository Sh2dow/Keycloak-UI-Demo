using backend.Application.Abstractions;
using backend.Application.Results;

namespace backend.Requests.Tasks;

public sealed record DeleteTaskCommand(Guid Id) : ICommand<Result<bool>>;
