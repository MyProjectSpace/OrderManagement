using System.Collections.Concurrent;
using MediatR;
using Microsoft.Extensions.Logging;
using Order.Orchestrator.Application.Abstractions;
using Shared.Contracts;

namespace Order.Orchestrator.Application.Commands;

public class EnqueuePendingSyncCommandHandler(
    IOrderEventPublisher publisher,
    ILogger<EnqueuePendingSyncCommandHandler> logger)
    : IRequestHandler<EnqueuePendingSyncCommand, EnqueuePendingSyncResult>
{
    private static readonly ConcurrentDictionary<string, byte> InFlight = new();

    public async Task<EnqueuePendingSyncResult> Handle(EnqueuePendingSyncCommand request, CancellationToken ct)
    {
        if (!InFlight.TryAdd(request.CorrelationId, 0))
        {
            logger.LogInformation("Coalescing concurrent ping for correlation {CorrelationId}", request.CorrelationId);
            return new EnqueuePendingSyncResult(request.CorrelationId, Coalesced: true);
        }

        try
        {
            await publisher.PublishAsync(new PendingSyncRequested(request.CorrelationId), ct);
            logger.LogInformation("Enqueued PendingSyncRequested for correlation {CorrelationId}", request.CorrelationId);
            return new EnqueuePendingSyncResult(request.CorrelationId, Coalesced: false);
        }
        finally
        {
            InFlight.TryRemove(request.CorrelationId, out _);
        }
    }
}
