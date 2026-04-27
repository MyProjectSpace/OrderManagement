using Shared.Contracts;

namespace Order.Orchestrator.Application.Abstractions;

public interface IOmsClient
{
    Task<IReadOnlyList<PendingOrder>> GetPendingOrdersAsync(string? correlationId, CancellationToken cancellationToken);
}
