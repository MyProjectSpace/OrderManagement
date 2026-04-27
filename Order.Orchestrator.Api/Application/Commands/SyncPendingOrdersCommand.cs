using MediatR;

namespace Order.Orchestrator.Application.Commands;

public record SyncPendingOrdersCommand(string CorrelationId) : IRequest<int>;
