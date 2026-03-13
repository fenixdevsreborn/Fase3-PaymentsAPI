using System.Net;
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
using Polly;
using Polly.Extensions.Http;

namespace Fcg.Payments.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var conn = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=fcg_payments;Username=postgres;Password=postgres";
        services.AddDbContext<PaymentsDbContext>(o => o.UseNpgsql(conn));
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
            .AddPolicyHandler(GetGamesRetryPolicy())
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10)));

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetGamesRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == HttpStatusCode.RequestTimeout || r.StatusCode == HttpStatusCode.GatewayTimeout)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(1.5, retryAttempt)));
    }
}
