using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Fcg.Payments.Api.Authentication;

/// <summary>Adds FCG JWT Bearer validation via Users API Authority and JWKS (RS256). No local signing key.</summary>
public static class JwtBearerExtensions
{
    public static IServiceCollection AddFcgJwtBearer(this IServiceCollection services)
    {
        services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerPostConfigureOptions>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        return services;
    }
}
