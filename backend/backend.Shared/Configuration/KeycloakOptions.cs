namespace backend.Configuration;

/// <summary>
/// Configuration options for Keycloak authentication.
/// </summary>
public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    /// <summary>
    /// Keycloak authority URL (realm endpoint).
    /// </summary>
    /// <remarks>Should be set via environment variable in production.</remarks>
    public string Authority { get; init; } = string.Empty;

    /// <summary>
    /// Optional metadata address for OpenID Connect discovery.
    /// If not specified, will be derived from Authority.
    /// </summary>
    public string? MetadataAddress { get; init; }
}
