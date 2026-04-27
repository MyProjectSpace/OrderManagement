namespace Order.Orchestrator.Infrastructure.Http;

public sealed class InventoryClientPoisonException(int statusCode, string title, string detail)
    : Exception($"Inventory rejected request ({statusCode} {title}): {detail}")
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
}
