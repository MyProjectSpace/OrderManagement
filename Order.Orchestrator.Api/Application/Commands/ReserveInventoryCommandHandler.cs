using MediatR;
using Microsoft.Extensions.Logging;
using Order.Orchestrator.Application.Abstractions;
using Order.Orchestrator.Domain;

namespace Order.Orchestrator.Application.Commands;

public class ReserveInventoryCommandHandler(
    IInventoryClient inventoryClient,
    IIdempotencyStore idempotencyStore,
    ILogger<ReserveInventoryCommandHandler> logger)
    : IRequestHandler<ReserveInventoryCommand, ReserveInventoryResult>
{
    public async Task<ReserveInventoryResult> Handle(ReserveInventoryCommand request, CancellationToken ct)
    {
        if (await idempotencyStore.HasProcessedAsync(request.OrderId, OperationType.Reserve, ct))
        {
            logger.LogInformation("Reserve already processed for order {OrderId}", request.OrderId);
            return new ReserveInventoryResult(request.OrderId, Accepted: true, AlreadyProcessed: true);
        }

        var result = await inventoryClient.ReserveAsync(request.OrderId, request.Items, request.CorrelationId, ct);
        await idempotencyStore.MarkProcessedAsync(request.OrderId, OperationType.Reserve, request.CorrelationId, ct);

        logger.LogInformation("Reserve completed for order {OrderId}: accepted={Accepted}", request.OrderId, result.Accepted);
        return new ReserveInventoryResult(request.OrderId, result.Accepted, AlreadyProcessed: false);
    }
}
