using Fcg.Payments.Application.Observability;

namespace Fcg.Payments.Api.Observability;

/// <summary>Implements Application's IObservabilityContextAccessor from current Activity (set by middleware).</summary>
public sealed class ObservabilityContextAccessor : IObservabilityContextAccessor
{
    public string? TraceId => ObservabilityContext.GetCurrentTraceId();
    public string? CorrelationId => ObservabilityContext.GetCurrentCorrelationId();
}
