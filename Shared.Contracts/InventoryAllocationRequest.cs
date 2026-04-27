namespace Shared.Contracts;

public record InventoryAllocationRequest(string OrderId, string[] Items);

public record InventoryOperationResult(string OrderId, string OperationType, bool Accepted);
