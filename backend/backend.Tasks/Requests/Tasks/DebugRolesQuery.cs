using backend.Application.Abstractions;

namespace backend.Requests.Tasks;

public sealed record DebugRolesQuery(IReadOnlyList<string> Roles) : IQuery<IReadOnlyList<string>>;
