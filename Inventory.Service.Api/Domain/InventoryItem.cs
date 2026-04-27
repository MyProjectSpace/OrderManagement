namespace Inventory.Service.Domain;

public class InventoryItem
{
    public string Sku { get; private set; } = default!;
    public int Available { get; private set; }
    public int Reserved { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private InventoryItem() { }

    public InventoryItem(string sku, int available)
    {
        Sku = sku;
        Available = available;
        Reserved = 0;
    }

    public void Apply(OperationType operation, int quantity)
    {
        if (quantity <= 0) throw new InvalidInventoryOperationException("Quantity must be positive.");

        switch (operation)
        {
            case OperationType.Allocate:
                if (Available < quantity)
                    throw new InsufficientStockException(Sku, Available, quantity);
                Available -= quantity;
                break;

            case OperationType.Reserve:
                if (Available < quantity)
                    throw new InsufficientStockException(Sku, Available, quantity);
                Available -= quantity;
                Reserved += quantity;
                break;

            default:
                throw new InvalidInventoryOperationException($"Unknown operation: {operation}");
        }
    }
}
