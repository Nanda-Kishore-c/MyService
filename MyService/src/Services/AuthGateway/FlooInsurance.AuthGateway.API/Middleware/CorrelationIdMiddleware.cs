using Serilog.Context;

namespace FlooInsurance.AuthGateway.API.Middleware;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate _next)
    {
        this._next = _next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId;

        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var incomingCorrelationId) &&
            !string.IsNullOrWhiteSpace(incomingCorrelationId))
        {
            correlationId = incomingCorrelationId.ToString();
        }
        else
        {
            correlationId = Guid.NewGuid().ToString("D");
        }

        context.TraceIdentifier = correlationId;
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
