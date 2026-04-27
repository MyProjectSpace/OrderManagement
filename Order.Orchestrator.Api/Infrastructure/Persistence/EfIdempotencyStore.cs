using Microsoft.EntityFrameworkCore;
using Order.Orchestrator.Application.Abstractions;
using Order.Orchestrator.Domain;

namespace Order.Orchestrator.Infrastructure.Persistence;

public class EfIdempotencyStore(OrchestratorDbContext db, TimeProvider clock) : IIdempotencyStore
{
    public Task<bool> HasProcessedAsync(string orderId, OperationType operationType, CancellationToken ct)
        => db.ProcessedMessages
            .AsNoTracking()
            .AnyAsync(p => p.OrderId == orderId && p.OperationType == operationType, ct);

    public async Task MarkProcessedAsync(string orderId, OperationType operationType, string? correlationId, CancellationToken ct)
    {
        db.ProcessedMessages.Add(new ProcessedMessage
        {
            OrderId = orderId,
            OperationType = operationType,
            CorrelationId = correlationId,
            ProcessedAtUtc = clock.GetUtcNow().UtcDateTime
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            var alreadyProcessed = await db.ProcessedMessages.AnyAsync(
                p => p.OrderId == orderId && p.OperationType == operationType, ct);
            if (!alreadyProcessed)
            {
                throw;
            }
        }
    }
}
