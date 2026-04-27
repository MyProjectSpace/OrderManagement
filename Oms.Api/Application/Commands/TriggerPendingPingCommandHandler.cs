using MediatR;
using Microsoft.Extensions.Logging;
using Oms.Api.Application.Abstractions;

namespace Oms.Api.Application.Commands;

public class TriggerPendingPingCommandHandler(
    IOrchestratorClient orchestrator,
    ILogger<TriggerPendingPingCommandHandler> logger)
    : IRequestHandler<TriggerPendingPingCommand, TriggerPendingPingResult>
{
    public async Task<TriggerPendingPingResult> Handle(TriggerPendingPingCommand request, CancellationToken ct)
    {
        var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId)
            ? Guid.NewGuid().ToString("N")
            : request.CorrelationId!;

        await orchestrator.PingPendingAsync(correlationId, ct);
        logger.LogInformation("Pinged Orchestrator pending-ping with correlation {CorrelationId}", correlationId);
        return new TriggerPendingPingResult(correlationId);
    }
}
