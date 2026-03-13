using Fcg.Payments.Api.Middleware;
using Fcg.Payments.Application.Extensions;
using Fcg.Payments.Infrastructure.Extensions;
using Fcg.Shared.Auth;
using Fcg.Shared.Observability;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddFcgJwtBearer(builder.Configuration);
builder.Services.AddFcgAuthorization();
builder.Services.AddProjectObservability(builder.Configuration, "Fcg.Payments.Api");
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Fcg.Payments.Infrastructure.Persistence.PaymentsDbContext>("db", tags: new[] { "ready" });
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<Fcg.Payments.Api.OpenApi.BearerSecuritySchemeTransformer>();
});

var app = builder.Build();

app.UseFcgObservability();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });

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

app.Run();

/// <summary>Exposed for integration tests (WebApplicationFactory).</summary>
public partial class Program { }
