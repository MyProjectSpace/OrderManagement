using MediatR;
using Microsoft.Extensions.Logging;
using Oms.Api.Application.Abstractions;
using Shared.Contracts;

namespace Oms.Api.Application.Queries;

public class GetPendingOrdersQueryHandler(
    IPendingOrderRepository repository,
    ILogger<GetPendingOrdersQueryHandler> logger)
    : IRequestHandler<GetPendingOrdersQuery, IReadOnlyList<PendingOrder>>
{
    public async Task<IReadOnlyList<PendingOrder>> Handle(GetPendingOrdersQuery request, CancellationToken ct)
    {
        var orders = await repository.GetPendingAsync(ct);
        logger.LogInformation("Returning {Count} pending orders", orders.Count);

        return orders
            .Select(o => new PendingOrder(o.OrderId, o.CustomerId, o.Items, o.Total))
            .ToList();
    }
}
