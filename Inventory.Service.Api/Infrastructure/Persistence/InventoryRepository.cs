using Inventory.Service.Application.Abstractions;
using Inventory.Service.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Service.Infrastructure.Persistence;

public class InventoryRepository(InventoryDbContext db) : IInventoryRepository
{
    public Task<InventoryItem?> GetItemAsync(string sku, CancellationToken ct) =>
        db.Items.FirstOrDefaultAsync(x => x.Sku == sku, ct);

    public Task<bool> OperationExistsAsync(string orderId, OperationType operationType, string sku, CancellationToken ct) =>
        db.Operations.AnyAsync(
            x => x.OrderId == orderId && x.OperationType == operationType && x.Sku == sku,
            ct);

    public async Task AddOperationAsync(InventoryOperation op, CancellationToken ct) =>
        await db.Operations.AddAsync(op, ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
