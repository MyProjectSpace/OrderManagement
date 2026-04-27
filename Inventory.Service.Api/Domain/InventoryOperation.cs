namespace Inventory.Service.Domain;

public class InventoryOperation
{
    public long Id { get; private set; }
    public string OrderId { get; private set; } = default!;
    public OperationType OperationType { get; private set; }
    public string Sku { get; private set; } = default!;
    public int Quantity { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    private InventoryOperation() { }

    public InventoryOperation(
        string orderId,
        OperationType operationType,
        string sku,
        int quantity,
        string? correlationId,
        DateTime occurredAtUtc)
    {
        OrderId = orderId;
        OperationType = operationType;
        Sku = sku;
        Quantity = quantity;
        CorrelationId = correlationId;
        OccurredAtUtc = occurredAtUtc;
    }
}
