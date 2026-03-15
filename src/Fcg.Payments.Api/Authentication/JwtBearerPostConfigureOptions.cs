using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Fcg.Payments.Api.Authentication;

/// <summary>Configures JwtBearerOptions from JwtOptions: Authority, metadata, audience, RS256, claim types. Handles metadata unavailability via logging.</summary>
public sealed class JwtBearerPostConfigureOptions : IPostConfigureOptions<JwtBearerOptions>
{
    private readonly IOptions<JwtOptions> _jwtOptions;
    private readonly ILogger<JwtBearerPostConfigureOptions> _logger;
    private readonly IServiceProvider _serviceProvider;

    public JwtBearerPostConfigureOptions(
        IOptions<JwtOptions> jwtOptions,
        ILogger<JwtBearerPostConfigureOptions> logger,
        IServiceProvider serviceProvider)
    {
        _jwtOptions = jwtOptions;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        var factory = _serviceProvider.GetService<IBackchannelHttpHandlerFactory>();
        if (factory != null)
            options.BackchannelHttpHandler = factory.Create();

        var jwt = _jwtOptions.Value;
        var authority = (jwt.Authority ?? "").TrimEnd('/');
        if (string.IsNullOrEmpty(authority))
            throw new InvalidOperationException("Jwt:Authority must be set (Users API base URL, e.g. https://users-api.example.com or https://localhost:5001).");

        options.Authority = authority;
        options.RequireHttpsMetadata = jwt.RequireHttpsMetadata;
        options.MetadataAddress = string.IsNullOrWhiteSpace(jwt.MetadataAddress)
            ? $"{authority}/.well-known/openid-configuration"
            : jwt.MetadataAddress.Trim();
        options.BackchannelTimeout = TimeSpan.FromSeconds(Math.Max(1, jwt.MetadataRequestTimeoutSeconds));

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidAudience = jwt.Audience,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 },
            NameClaimType = FcgClaimTypes.Name,
            RoleClaimType = FcgClaimTypes.Role,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events ??= new JwtBearerEvents();
        var existingFailed = options.Events.OnAuthenticationFailed;
        options.Events.OnAuthenticationFailed = async ctx =>
        {
            _logger.LogWarning(ctx.Exception, "JWT validation failed: {Message}", ctx.Exception?.Message);
            if (existingFailed != null) await existingFailed(ctx);
        };

        var existingChallenge = options.Events.OnChallenge;
        options.Events.OnChallenge = async ctx =>
        {
            if (ctx.AuthenticateFailure != null)
                _logger.LogDebug(ctx.AuthenticateFailure, "JWT challenge: {Error}", ctx.Error);
            if (existingChallenge != null) await existingChallenge(ctx);
        };
    }
}
