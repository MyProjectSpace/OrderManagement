using Oms.Api.Domain;

namespace Oms.Api.Application.Abstractions;

public interface IPendingOrderRepository
{
    Task<IReadOnlyList<PendingOrder>> GetPendingAsync(CancellationToken cancellationToken);
}
