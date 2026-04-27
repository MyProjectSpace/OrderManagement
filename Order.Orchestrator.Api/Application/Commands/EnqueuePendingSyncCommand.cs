using MediatR;

namespace Order.Orchestrator.Application.Commands;

public record EnqueuePendingSyncCommand(string CorrelationId) : IRequest<EnqueuePendingSyncResult>;

public record EnqueuePendingSyncResult(string CorrelationId, bool Coalesced);
