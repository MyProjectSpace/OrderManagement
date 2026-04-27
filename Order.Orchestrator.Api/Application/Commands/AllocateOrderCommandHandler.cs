using MediatR;
using Microsoft.Extensions.Logging;
using Order.Orchestrator.Application.Abstractions;
using Order.Orchestrator.Domain;

namespace Order.Orchestrator.Application.Commands;

public class AllocateOrderCommandHandler(
    IInventoryClient inventoryClient,
    IIdempotencyStore idempotencyStore,
    ILogger<AllocateOrderCommandHandler> logger)
    : IRequestHandler<AllocateOrderCommand, AllocateOrderResult>
{
    public async Task<AllocateOrderResult> Handle(AllocateOrderCommand request, CancellationToken ct)
    {
        if (await idempotencyStore.HasProcessedAsync(request.OrderId, OperationType.Allocate, ct))
        {
            logger.LogInformation("Allocate already processed for order {OrderId}", request.OrderId);
            return new AllocateOrderResult(request.OrderId, Accepted: true, AlreadyProcessed: true);
        }

        var result = await inventoryClient.AllocateAsync(request.OrderId, request.Items, request.CorrelationId, ct);
        await idempotencyStore.MarkProcessedAsync(request.OrderId, OperationType.Allocate, request.CorrelationId, ct);

        logger.LogInformation("Allocate completed for order {OrderId}: accepted={Accepted}", request.OrderId, result.Accepted);
        return new AllocateOrderResult(request.OrderId, result.Accepted, AlreadyProcessed: false);
    }
}
