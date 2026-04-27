using System.Net.Http.Json;
using Order.Orchestrator.Application.Abstractions;
using Shared.Contracts;

namespace Order.Orchestrator.Infrastructure.Http;

public class InventoryHttpClient(HttpClient httpClient) : IInventoryClient
{
    public Task<InventoryOperationResult> AllocateAsync(string orderId, string[] items, string? correlationId, CancellationToken ct)
        => SendAsync("/inventory/allocate", orderId, items, correlationId, ct);

    public Task<InventoryOperationResult> ReserveAsync(string orderId, string[] items, string? correlationId, CancellationToken ct)
        => SendAsync("/inventory/reserve", orderId, items, correlationId, ct);

    private async Task<InventoryOperationResult> SendAsync(string path, string orderId, string[] items, string? correlationId, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(new InventoryAllocationRequest(orderId, items))
        };
        if (!string.IsNullOrEmpty(correlationId))
        {
            request.Headers.Add("X-Correlation-Id", correlationId);
        }

        using var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<InventoryOperationResult>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("Empty inventory response");
    }
}
