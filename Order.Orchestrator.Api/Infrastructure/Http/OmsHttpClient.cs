using System.Net.Http.Json;
using Order.Orchestrator.Application.Abstractions;
using Shared.Contracts;

namespace Order.Orchestrator.Infrastructure.Http;

public class OmsHttpClient(HttpClient httpClient) : IOmsClient
{
    public async Task<IReadOnlyList<PendingOrder>> GetPendingOrdersAsync(string? correlationId, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/orders/pending");
        if (!string.IsNullOrEmpty(correlationId))
        {
            request.Headers.Add("X-Correlation-Id", correlationId);
        }

        using var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var orders = await response.Content.ReadFromJsonAsync<List<PendingOrder>>(cancellationToken: ct);
        return orders ?? new List<PendingOrder>();
    }
}
