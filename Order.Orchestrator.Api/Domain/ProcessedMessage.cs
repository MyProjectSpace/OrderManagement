namespace Order.Orchestrator.Domain;

public class ProcessedMessage
{
    public long Id { get; set; }
    public string? CorrelationId { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public OperationType OperationType { get; set; }
    public DateTime ProcessedAtUtc { get; set; }
}
