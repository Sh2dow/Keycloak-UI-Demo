using backend.Domain.Models;
using backend.Shared.Application.Exceptions;
using backend.Shared.Application.Users;

namespace backend.Infrastructure.Application.Users;

public sealed class EffectiveUserAccessor : IEffectiveUserAccessor
{
    private readonly IUserDirectory _userDirectory;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private AppUser? _cachedUser;

    public EffectiveUserAccessor(
        IUserDirectory userDirectory,
        ICurrentUserAccessor currentUser,
        IHttpContextAccessor httpContextAccessor)
    {
        _userDirectory = userDirectory;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Guid> GetUserIdAsync(CancellationToken ct = default)
    {
        var user = await GetUserAsync(ct);
        return user.Id;
    }

    public async Task<AppUser> GetUserAsync(CancellationToken ct = default)
    {
        if (_cachedUser != null) return _cachedUser;

        var sub = _currentUser.Subject;
        if (string.IsNullOrWhiteSpace(sub))
        {
            // Return a default user for anonymous requests
            var defaultUser = await _userDirectory.EnsureAsync("anonymous", "Anonymous", null, ct);
            _cachedUser = defaultUser;
            return _cachedUser;
        }

        var currentUser = await _userDirectory.EnsureAsync(sub, _currentUser.PreferredUsername, _currentUser.Email, ct);

        var rawAsUserId = _httpContextAccessor.HttpContext?.Request.Query["asUserId"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(rawAsUserId))
        {
            _cachedUser = currentUser;
            return _cachedUser;
        }

        if (!Guid.TryParse(rawAsUserId, out var asUserId))
        {
            throw new HttpProblemException(
                StatusCodes.Status400BadRequest,
                "Invalid asUserId",
                "The asUserId query parameter must be a GUID.");
        }

        if (asUserId == currentUser.Id)
        {
            _cachedUser = currentUser;
            return _cachedUser;
        }

        if (!_currentUser.IsInRole("admin"))
        {
            throw new HttpProblemException(StatusCodes.Status403Forbidden, "Forbidden", "Admin role is required for impersonation.");
        }

        var targetUser = await _userDirectory.FindByIdAsync(asUserId, ct);

        if (targetUser == null)
        {
            throw new HttpProblemException(StatusCodes.Status404NotFound, "Not Found", "Target user not found.");
        }

        _cachedUser = targetUser;
        return _cachedUser;
    }
}
