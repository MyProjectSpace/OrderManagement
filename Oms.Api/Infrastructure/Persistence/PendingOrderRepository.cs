using Microsoft.EntityFrameworkCore;
using Oms.Api.Application.Abstractions;
using Oms.Api.Domain;

namespace Oms.Api.Infrastructure.Persistence;

public class PendingOrderRepository(OmsDbContext db) : IPendingOrderRepository
{
    public async Task<IReadOnlyList<PendingOrder>> GetPendingAsync(CancellationToken ct)
    {
        return await db.PendingOrders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Pending)
            .ToListAsync(ct);
    }
}
