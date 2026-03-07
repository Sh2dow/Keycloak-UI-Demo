using backend.Requests.Tasks;
using MediatR;

namespace backend.Handlers.Tasks;

public sealed class DebugRolesHandler : IRequestHandler<DebugRolesQuery, IReadOnlyList<string>>
{
    public Task<IReadOnlyList<string>> Handle(DebugRolesQuery req, CancellationToken ct)
    {
        return Task.FromResult(req.Roles);
    }
}
