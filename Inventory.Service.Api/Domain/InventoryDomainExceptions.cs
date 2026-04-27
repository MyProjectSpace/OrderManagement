namespace Inventory.Service.Domain;

public class InvalidInventoryOperationException(string message) : Exception(message);

public class InsufficientStockException(string sku, int available, int requested)
    : Exception($"Insufficient stock for {sku}: available={available}, requested={requested}")
{
    public string Sku { get; } = sku;
    public int Available { get; } = available;
    public int Requested { get; } = requested;
}

public class UnknownSkuException(string sku) : Exception($"Unknown SKU: {sku}")
{
    public string Sku { get; } = sku;
}
