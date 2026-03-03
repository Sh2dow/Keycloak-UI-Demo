using MediatR;

namespace backend.Requests.Tasks;

public sealed record DebugRolesQuery(IReadOnlyList<string> Roles) : IRequest<IReadOnlyList<string>>;
