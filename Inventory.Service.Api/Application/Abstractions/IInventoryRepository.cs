using Inventory.Service.Domain;

namespace Inventory.Service.Application.Abstractions;

public interface IInventoryRepository
{
    Task<InventoryItem?> GetItemAsync(string sku, CancellationToken ct);
    Task<bool> OperationExistsAsync(string orderId, OperationType operationType, string sku, CancellationToken ct);
    Task AddOperationAsync(InventoryOperation op, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
