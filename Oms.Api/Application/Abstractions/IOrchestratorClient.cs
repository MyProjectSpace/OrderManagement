namespace Oms.Api.Application.Abstractions;

public interface IOrchestratorClient
{
    Task PingPendingAsync(string correlationId, CancellationToken cancellationToken);
}
