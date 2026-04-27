using Inventory.Service.Application.Abstractions;
using Inventory.Service.Domain;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace Inventory.Service.Application.Commands;

public record ApplyInventoryCommand(
    string OrderId,
    string[] Items,
    OperationType OperationType,
    string? CorrelationId) : IRequest<InventoryOperationResult>;

public class ApplyInventoryCommandHandler(
    IInventoryRepository repository,
    TimeProvider clock,
    ILogger<ApplyInventoryCommandHandler> logger)
    : IRequestHandler<ApplyInventoryCommand, InventoryOperationResult>
{
    public async Task<InventoryOperationResult> Handle(ApplyInventoryCommand request, CancellationToken ct)
    {
        var grouped = request.Items
            .GroupBy(sku => sku)
            .Select(g => (Sku: g.Key, Quantity: g.Count()));

        foreach (var (sku, quantity) in grouped)
        {
            var alreadyApplied = await repository.OperationExistsAsync(
                request.OrderId, request.OperationType, sku, ct);

            if (alreadyApplied)
            {
                logger.LogInformation(
                    "Idempotent skip: {Operation} for {OrderId}/{Sku} already applied",
                    request.OperationType, request.OrderId, sku);
                continue;
            }

            var item = await repository.GetItemAsync(sku, ct)
                ?? throw new UnknownSkuException(sku);

            item.Apply(request.OperationType, quantity);

            await repository.AddOperationAsync(
                new InventoryOperation(
                    request.OrderId,
                    request.OperationType,
                    sku,
                    quantity,
                    request.CorrelationId,
                    clock.GetUtcNow().UtcDateTime),
                ct);
        }

        await repository.SaveChangesAsync(ct);

        return new InventoryOperationResult(
            request.OrderId,
            request.OperationType.ToString(),
            Accepted: true);
    }
}
