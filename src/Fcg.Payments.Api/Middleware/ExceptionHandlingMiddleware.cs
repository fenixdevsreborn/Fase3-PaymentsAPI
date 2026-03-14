using System.Net;
using System.Text.Json;
using Fcg.Payments.Api.Observability;
using Fcg.Payments.Application.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Fcg.Payments.Api.Middleware;

/// <summary>Maps domain exceptions to HTTP JSON. Logs with TraceId and CorrelationId.</summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Not found. {TraceId} {CorrelationId}", ObservabilityContext.GetCurrentTraceId(), ObservabilityContext.GetCurrentCorrelationId());
            await WriteResponseAsync(context, (int)HttpStatusCode.NotFound, "Resource not found").ConfigureAwait(false);
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict. {TraceId} {CorrelationId}", ObservabilityContext.GetCurrentTraceId(), ObservabilityContext.GetCurrentCorrelationId());
            await WriteResponseAsync(context, (int)HttpStatusCode.Conflict, "Conflict").ConfigureAwait(false);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Bad request. {TraceId} {CorrelationId}", ObservabilityContext.GetCurrentTraceId(), ObservabilityContext.GetCurrentCorrelationId());
            await WriteResponseAsync(context, (int)HttpStatusCode.BadRequest, "Bad request").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception. {TraceId} {CorrelationId} {Message}", ObservabilityContext.GetCurrentTraceId(), ObservabilityContext.GetCurrentCorrelationId(), ex.Message);
            await WriteResponseAsync(context, (int)HttpStatusCode.InternalServerError, "An error occurred.").ConfigureAwait(false);
        }
    }

    private static async Task WriteResponseAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { message })).ConfigureAwait(false);
    }
}
