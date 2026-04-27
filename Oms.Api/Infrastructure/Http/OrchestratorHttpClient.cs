using System.Net.Http.Json;
using Oms.Api.Application.Abstractions;
using Shared.Contracts;

namespace Oms.Api.Infrastructure.Http;

public class OrchestratorHttpClient(HttpClient httpClient) : IOrchestratorClient
{
    public async Task PingPendingAsync(string correlationId, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/orders/pending-ping")
        {
            Content = JsonContent.Create(new PendingPingRequest(correlationId))
        };
        request.Headers.Add("X-Correlation-Id", correlationId);

        using var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }
}
