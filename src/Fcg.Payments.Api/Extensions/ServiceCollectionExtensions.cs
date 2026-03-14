using Fcg.Payments.Api.Authentication;
using Fcg.Payments.Api.Authorization;
using Fcg.Payments.Api.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fcg.Payments.Api.Extensions;

/// <summary>Combined DI for Payments API: JWT, Authorization, Observability, CurrentUser.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentsApiAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFcgJwtBearer(configuration);
        services.AddFcgAuthorization();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
        return services;
    }

    public static IServiceCollection AddPaymentsApiObservability(this IServiceCollection services, IConfiguration configuration, string projectName = "Fcg.Payments.Api")
    {
        services.AddProjectObservability(configuration, projectName);
        return services;
    }
}
