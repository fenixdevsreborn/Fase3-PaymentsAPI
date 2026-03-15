using Amazon.SQS;
using Fcg.Payments.Application.Services;
using Fcg.Payments.Domain.Repositories;
using Fcg.Payments.Infrastructure.Gateways;
using Fcg.Payments.Infrastructure.Http;
using Fcg.Payments.Infrastructure.Outbox;
using Fcg.Payments.Infrastructure.Persistence;
using Fcg.Payments.Infrastructure.Repositories;
using Fcg.Payments.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;

namespace Fcg.Payments.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment? hostEnvironment = null)
    {
        var useInMemory = configuration.GetValue<bool>("UseInMemoryDatabase")
            || string.Equals(hostEnvironment?.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase);

        if (useInMemory)
            services.AddDbContext<PaymentsDbContext>(o => o.UseInMemoryDatabase("FcgPaymentsTests"));
        else
        {
            var conn = configuration.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Database=fcg_payments;Username=postgres;Password=postgres";
            services.AddDbContext<PaymentsDbContext>(o => o.UseNpgsql(conn));
        }
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        services.AddScoped<IPaymentGateway, FakePaymentGateway>();
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();
        services.AddScoped<IUserInfoService, UserInfoService>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();

        services.Configure<OutboxRelayOptions>(configuration.GetSection(OutboxRelayOptions.SectionName));
        if (!string.IsNullOrEmpty(configuration[OutboxRelayOptions.SectionName + ":QueueUrl"]))
        {
            services.AddSingleton<IAmazonSQS, AmazonSQSClient>();
            services.AddSingleton<IOutboxSqsSender, OutboxSqsSender>();
            services.AddHostedService<OutboxRelayService>();
        }

        services.Configure<GamesApiOptions>(configuration.GetSection(GamesApiOptions.SectionName));
        var gamesBaseUrl = configuration["GamesApi:BaseUrl"] ?? "http://localhost:5001";
        services.AddHttpClient<IGameApiClient, GameApiClient>(c =>
        {
            c.BaseAddress = new Uri(gamesBaseUrl.TrimEnd('/') + "/");
            c.Timeout = TimeSpan.FromSeconds(10);
        })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.UseJitter = true;
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
            });

        return services;
    }
}
