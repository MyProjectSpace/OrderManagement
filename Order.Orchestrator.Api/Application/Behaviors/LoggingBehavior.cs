using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Order.Orchestrator.Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        var correlationId = Activity.Current?.GetBaggageItem("correlationId");

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["RequestType"] = name,
            ["CorrelationId"] = correlationId
        });

        var sw = Stopwatch.StartNew();
        try
        {
            logger.LogInformation("Handling {Request}", name);
            var response = await next();
            sw.Stop();
            logger.LogInformation("Handled {Request} in {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Failed {Request} after {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
