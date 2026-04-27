using MediatR;
using Microsoft.Extensions.Logging;
using Order.Orchestrator.Application.Abstractions;
using Shared.Contracts;

namespace Order.Orchestrator.Application.Commands;

public class SyncPendingOrdersCommandHandler(
    IOmsClient omsClient,
    IOrderEventPublisher publisher,
    ILogger<SyncPendingOrdersCommandHandler> logger)
    : IRequestHandler<SyncPendingOrdersCommand, int>
{
    public async Task<int> Handle(SyncPendingOrdersCommand request, CancellationToken ct)
    {
        var orders = await omsClient.GetPendingOrdersAsync(request.CorrelationId, ct);
        logger.LogInformation("Fetched {Count} pending orders for correlation {CorrelationId}", orders.Count, request.CorrelationId);

        foreach (var order in orders)
        {
            await publisher.SendAsync(
                new AllocateOrderRequested(request.CorrelationId, order.OrderId, order.CustomerId, order.Items),
                ct);
        }

        return orders.Count;
    }
}
