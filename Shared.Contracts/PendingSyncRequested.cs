namespace Shared.Contracts;

public record PendingSyncRequested(string CorrelationId);

public record AllocateOrderRequested(
    string CorrelationId,
    string OrderId,
    string CustomerId,
    string[] Items);
