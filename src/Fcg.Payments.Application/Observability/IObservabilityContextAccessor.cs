namespace Fcg.Payments.Application.Observability;

/// <summary>Access to current trace and correlation id. Implemented in Api layer; used by Application for audit and events.</summary>
public interface IObservabilityContextAccessor
{
    string? TraceId { get; }
    string? CorrelationId { get; }
}
