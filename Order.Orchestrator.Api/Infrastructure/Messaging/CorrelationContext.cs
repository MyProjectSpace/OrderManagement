using Order.Orchestrator.Application.Abstractions;

namespace Order.Orchestrator.Infrastructure.Messaging;

public class CorrelationContext : ICorrelationContext
{
    private readonly AsyncLocal<string?> _correlationId = new();

    public string? CorrelationId => _correlationId.Value;

    public void SetCorrelationId(string? correlationId) => _correlationId.Value = correlationId;
}
