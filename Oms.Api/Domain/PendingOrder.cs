namespace Oms.Api.Domain;

public class PendingOrder
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string[] Items { get; set; } = [];
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
