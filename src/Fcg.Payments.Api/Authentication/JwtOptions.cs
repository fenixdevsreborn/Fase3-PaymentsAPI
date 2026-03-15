namespace Fcg.Payments.Api.Authentication;

/// <summary>JWT validation configuration: Authority (Users API URL), Audience, metadata and backchannel. No signing key — keys from JWKS.</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Issuer/Authority URL of the Users API (e.g. https://users-api.example.com). Metadata at Authority/.well-known/openid-configuration.</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>Explicit metadata URL; if empty, derived from Authority + /.well-known/openid-configuration.</summary>
    public string? MetadataAddress { get; set; }

    /// <summary>Expected audience (e.g. fcg-cloud-platform). Must match Users API.</summary>
    public string Audience { get; set; } = "fcg-cloud-platform";

    /// <summary>Require HTTPS for metadata. Set false only for local development (e.g. http://localhost).</summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>Timeout for metadata/JWKS HTTP requests.</summary>
    public int MetadataRequestTimeoutSeconds { get; set; } = 10;
}
