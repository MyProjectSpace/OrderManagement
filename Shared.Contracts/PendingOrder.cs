namespace Shared.Contracts;

public record PendingOrder(string OrderId, string CustomerId, string[] Items, decimal Total);
