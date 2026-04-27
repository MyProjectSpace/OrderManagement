using MediatR;

namespace Order.Orchestrator.Application.Commands;

public record AllocateOrderCommand(
    string OrderId,
    string CustomerId,
    string[] Items,
    string? CorrelationId) : IRequest<AllocateOrderResult>;

public record AllocateOrderResult(string OrderId, bool Accepted, bool AlreadyProcessed);
