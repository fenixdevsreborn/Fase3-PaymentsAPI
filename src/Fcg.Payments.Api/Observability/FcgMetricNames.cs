namespace Fcg.Payments.Api.Observability;

/// <summary>Metric names (snake_case) for Payments API.</summary>
public static class FcgMetricNames
{
    public const string HttpServerRequestCount = "http.server.request.count";
    public const string HttpServerRequestDuration = "http.server.request.duration";
    public const string HttpServerActiveRequests = "http.server.active_requests";
    public const string PaymentsCreated = "payments.created";
    public const string PaymentsPaid = "payments.paid";
    public const string PaymentsFailed = "payments.failed";
    public const string ExceptionsCount = "exceptions.count";

    public const string TagHttpMethod = "http.request.method";
    public const string TagHttpRoute = "http.route";
    public const string TagHttpStatusCode = "http.response.status_code";
    public const string TagExceptionType = "exception.type";
}
