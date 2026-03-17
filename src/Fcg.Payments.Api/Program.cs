using Fcg.Payments.Api.Extensions;
using Fcg.Payments.Api.Middleware;
using Fcg.Payments.Api.Observability;
using Fcg.Payments.Application.Extensions;
using Fcg.Payments.Infrastructure.Extensions;
using Fcg.Payments.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddApplication();
builder.Services.AddPaymentsApiAuth(builder.Configuration);
builder.Services.AddPaymentsApiObservability(builder.Configuration, "Fcg.Payments.Api");
builder.Services.AddOpenTelemetryObservability(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PaymentsDbContext>("db", tags: new[] { "ready" });
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<Fcg.Payments.Api.OpenApi.BearerSecuritySchemeTransformer>();
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseFcgObservability();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });

var enableOpenApi = !app.Environment.IsProduction() || app.Configuration.GetValue<bool>("EnableOpenApi");
if (enableOpenApi)
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("FCG Payments API").WithTheme(ScalarTheme.BluePlanet);
    });
}

app.MapGet("/api/discovery", (HttpContext ctx) => new
{
    service = "Fcg.Payments.Api",
    basePath = ctx.Request.PathBase.Value?.TrimEnd('/') ?? "",
    openApiUrl = $"{ctx.Request.PathBase.Value?.TrimEnd('/')}/openapi/v1.json",
    docsUrl = $"{ctx.Request.PathBase.Value?.TrimEnd('/')}/scalar/v1",
    healthUrl = $"{ctx.Request.PathBase.Value?.TrimEnd('/')}/health"
}).AllowAnonymous();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    if (!config.GetValue<bool>("UseInMemoryDatabase"))
    {
        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
        await PostgresDatabaseEnsurer.EnsureExistsAsync(connectionString);
        await db.Database.MigrateAsync();
    }
}

app.Run();

/// <summary>Exposed for integration tests (WebApplicationFactory).</summary>
public partial class Program { }
