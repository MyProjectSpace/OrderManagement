using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Order.Orchestrator.Application.Commands;
using Shared.Contracts;

namespace Order.Orchestrator.Infrastructure.Consumers;

public class PendingSyncRequestedConsumer(
    ISender mediator,
    ILogger<PendingSyncRequestedConsumer> logger) : IConsumer<PendingSyncRequested>
{
    public async Task Consume(ConsumeContext<PendingSyncRequested> context)
    {
        var msg = context.Message;
        logger.LogInformation("Consuming PendingSyncRequested for correlation {CorrelationId}", msg.CorrelationId);
        await mediator.Send(new SyncPendingOrdersCommand(msg.CorrelationId), context.CancellationToken);
    }
}
