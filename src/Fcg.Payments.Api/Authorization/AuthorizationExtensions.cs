using Fcg.Payments.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Fcg.Payments.Api.Authorization;

/// <summary>Registers FCG authorization policies: RequireAuthenticatedUser, RequireAdmin.</summary>
public static class AuthorizationExtensions
{
    public static IServiceCollection AddFcgAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(FcgPolicies.RequireAuthenticatedUser, policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy(FcgPolicies.RequireAdmin, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireRole(FcgRoles.Admin));
        });
        return services;
    }
}
