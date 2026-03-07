using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace backend.Application.Security;

public sealed class KeycloakRoleClaimsTransformation : IClaimsTransformation
{
    private readonly ILogger<KeycloakRoleClaimsTransformation> _logger;

    public KeycloakRoleClaimsTransformation(ILogger<KeycloakRoleClaimsTransformation> logger)
    {
        _logger = logger;
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return Task.FromResult(principal);
        }

        var realmAccess = principal.FindFirst("realm_access")?.Value;
        if (string.IsNullOrWhiteSpace(realmAccess))
        {
            return Task.FromResult(principal);
        }

        try
        {
            using var doc = JsonDocument.Parse(realmAccess);
            if (!doc.RootElement.TryGetProperty("roles", out var roles))
            {
                return Task.FromResult(principal);
            }

            foreach (var role in roles.EnumerateArray())
            {
                var roleValue = role.GetString();
                if (string.IsNullOrWhiteSpace(roleValue)) continue;

                var hasRole = principal.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == roleValue);
                if (!hasRole)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to transform Keycloak realm roles.");
        }

        return Task.FromResult(principal);
    }
}
