using Order.Orchestrator.Domain;

namespace Order.Orchestrator.Application.Abstractions;

public interface IIdempotencyStore
{
    Task<bool> HasProcessedAsync(string orderId, OperationType operationType, CancellationToken cancellationToken);
    Task MarkProcessedAsync(string orderId, OperationType operationType, string? correlationId, CancellationToken cancellationToken);
}
