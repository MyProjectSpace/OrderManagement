using Shared.Contracts;

namespace Order.Orchestrator.Application.Abstractions;

public interface IInventoryClient
{
    Task<InventoryOperationResult> AllocateAsync(string orderId, string[] items, string? correlationId, CancellationToken cancellationToken);
    Task<InventoryOperationResult> ReserveAsync(string orderId, string[] items, string? correlationId, CancellationToken cancellationToken);
}
