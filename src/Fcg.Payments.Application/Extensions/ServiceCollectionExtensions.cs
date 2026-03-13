using Fcg.Payments.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Fcg.Payments.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IAuditService, AuditService>();
        return services;
    }
}
