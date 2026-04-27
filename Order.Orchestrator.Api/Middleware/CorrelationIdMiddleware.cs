using System.Diagnostics;
using Order.Orchestrator.Application.Abstractions;
using Serilog.Context;

namespace Order.Orchestrator.Api.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context, ICorrelationContext correlationContext)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        correlationContext.SetCorrelationId(correlationId);
        Activity.Current?.AddBaggage("correlationId", correlationId);
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
