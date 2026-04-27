using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Order.Orchestrator.Tests.Integration.Infrastructure;
using Shared.Contracts;
using InventoryDomain = Inventory.Service.Domain.OperationType;

namespace Order.Orchestrator.Tests.Integration;

[Collection("Integration")]
public class Flow1Tests : IAsyncLifetime
{
    private readonly OmsTestFactory _oms = new();
    private readonly InventoryTestFactory _inventory = new();
    private OrchestratorTestFactory _orchestrator = null!;

    public async Task InitializeAsync()
    {
        // Force startup so seeders run.
        var omsHandler = _oms.Server.CreateHandler();
        var inventoryHandler = _inventory.Server.CreateHandler();
        _ = _oms.CreateClient();
        _ = _inventory.CreateClient();

        _orchestrator = new OrchestratorTestFactory()
            .WithDownstreamUrls(_oms.Server.BaseAddress.ToString(), _inventory.Server.BaseAddress.ToString())
            .WithServices(services =>
            {
                services.PostConfigure<HttpClientFactoryOptions>("IOmsClient", o =>
                    o.HttpMessageHandlerBuilderActions.Add(b => b.PrimaryHandler = omsHandler));
                services.PostConfigure<HttpClientFactoryOptions>("IInventoryClient", o =>
                    o.HttpMessageHandlerBuilderActions.Add(b => b.PrimaryHandler = inventoryHandler));
            });
        _ = _orchestrator.CreateClient();

        await Task.CompletedTask;
    }

    [Fact]
    public async Task Flow1_OmsPing_InventoryReceives()
    {
        var client = _orchestrator.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/orders/pending-ping",
            new PendingPingRequest($"flow1-{Guid.NewGuid():N}"));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var pendingOrderIds = new[] { "ORD-1001", "ORD-1002", "ORD-1003", "ORD-1004", "ORD-1005" };

        await TestPolling.UntilAsync(
            async () =>
            {
                using var scope = _inventory.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<Inventory.Service.Infrastructure.Persistence.InventoryDbContext>();
                var processed = await db.Operations
                    .Where(o => o.OperationType == InventoryDomain.Allocate)
                    .Select(o => o.OrderId)
                    .Distinct()
                    .ToListAsync();
                return pendingOrderIds.All(id => processed.Contains(id));
            },
            timeout: TimeSpan.FromSeconds(30),
            description: "Inventory should receive Allocate calls for all 5 pending orders");
    }

    public async Task DisposeAsync()
    {
        _orchestrator?.Dispose();
        _oms.Dispose();
        _inventory.Dispose();
        await Task.CompletedTask;
    }
}
