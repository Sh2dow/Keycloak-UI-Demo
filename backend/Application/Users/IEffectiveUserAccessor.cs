using backend.Models;

namespace backend.Application.Users;

public interface IEffectiveUserAccessor
{
    Task<Guid> GetUserIdAsync(CancellationToken ct = default);
    Task<AppUser> GetUserAsync(CancellationToken ct = default);
}
