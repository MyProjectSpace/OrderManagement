using System.Diagnostics;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Order.Orchestrator.Tests.Integration.Infrastructure;
using Shared.Contracts;
using InventoryDomain = Inventory.Service.Domain.OperationType;

namespace Order.Orchestrator.Tests.Stress;

[Trait("Category", "Stress")]
[Collection("Integration")]
public class QueueStressTests : IAsyncLifetime
{
    private const int MessageCount = 100;
    private static readonly TimeSpan TargetDuration = TimeSpan.FromSeconds(60);

    private readonly InventoryTestFactory _inventory = new();
    private OrchestratorTestFactory _orchestrator = null!;

    public async Task InitializeAsync()
    {
        var inventoryHandler = _inventory.Server.CreateHandler();
        _ = _inventory.CreateClient();

        _orchestrator = new OrchestratorTestFactory()
            .WithDownstreamUrls(omsBaseAddress: null, inventoryBaseAddress: _inventory.Server.BaseAddress.ToString())
            .WithServices(services =>
            {
                services.PostConfigure<HttpClientFactoryOptions>("IInventoryClient", o =>
                    o.HttpMessageHandlerBuilderActions.Add(b => b.PrimaryHandler = inventoryHandler));
            });
        _ = _orchestrator.CreateClient();

        await Task.CompletedTask;
    }

    [Fact]
    public async Task Queue_StressTest_100MessagesProcessedWithinTargetTime()
    {
        var bus = _orchestrator.Services.GetRequiredService<IBus>();
        var orderIds = Enumerable.Range(1, MessageCount).Select(i => $"STRESS-{i:0000}").ToArray();

        var sw = Stopwatch.StartNew();

        await Task.WhenAll(orderIds.Select(id => bus.Publish(new PaymentConfirmedEvent(
            id, $"CUST-{id}", ["ITEM-F"], 1.00m, DateTime.UtcNow))));

        await TestPolling.UntilAsync(
            async () =>
            {
                using var scope = _inventory.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<Inventory.Service.Infrastructure.Persistence.InventoryDbContext>();
                var processed = await db.Operations
                    .Where(o => o.OperationType == InventoryDomain.Reserve && o.OrderId.StartsWith("STRESS-"))
                    .CountAsync();
                return processed >= MessageCount;
            },
            timeout: TargetDuration,
            pollInterval: TimeSpan.FromMilliseconds(250),
            description: $"All {MessageCount} Reserve operations should land in Inventory within {TargetDuration.TotalSeconds}s");

        sw.Stop();

        // Final verification + emit timing for the report.
        using var scope = _inventory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Inventory.Service.Infrastructure.Persistence.InventoryDbContext>();
        var distinct = await db.Operations
            .Where(o => o.OperationType == InventoryDomain.Reserve && o.OrderId.StartsWith("STRESS-"))
            .Select(o => o.OrderId)
            .Distinct()
            .CountAsync();

        distinct.Should().Be(MessageCount);
        sw.Elapsed.Should().BeLessThan(TargetDuration);
    }

    public async Task DisposeAsync()
    {
        _orchestrator?.Dispose();
        _inventory.Dispose();
        await Task.CompletedTask;
    }
}
