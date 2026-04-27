using MediatR;

namespace Order.Orchestrator.Application.Commands;

public record ReserveInventoryCommand(
    string OrderId,
    string CustomerId,
    string[] Items,
    string? CorrelationId) : IRequest<ReserveInventoryResult>;

public record ReserveInventoryResult(string OrderId, bool Accepted, bool AlreadyProcessed);
