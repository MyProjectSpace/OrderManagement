using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Order.Orchestrator.Tests.Integration.Infrastructure;
using Shared.Contracts;
using InventoryDomain = Inventory.Service.Domain.OperationType;

namespace Order.Orchestrator.Tests.Integration;

[Collection("Integration")]
public class Flow2Tests : IAsyncLifetime
{
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
    public async Task Flow2_PaymentConfirmed_InventoryReceives()
    {
        // Note: MassTransit InMemory buses are isolated per IServiceProvider,
        // so the Payment Gateway factory's bus cannot reach the Orchestrator's
        // bus across separate factories. We publish the event directly on
        // the Orchestrator's bus, which is what RabbitMQ would deliver in
        // production. The Payment Gateway HTTP → publish hop is covered by
        // PublishPaymentConfirmedCommandHandler unit tests.
        // Use IBus (not IPublishEndpoint) to bypass the EF outbox — we want
        // the message on the wire immediately, simulating what RabbitMQ
        // delivery from Payment Gateway would do in production.
        var bus = _orchestrator.Services.GetRequiredService<IBus>();
        await bus.Publish(new PaymentConfirmedEvent(
            "ORD-FLOW2",
            "CUST-FLOW2",
            ["ITEM-F"],
            99.99m,
            DateTime.UtcNow));

        await TestPolling.UntilAsync(
            async () =>
            {
                using var scope = _inventory.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<Inventory.Service.Infrastructure.Persistence.InventoryDbContext>();
                return await db.Operations.AnyAsync(o =>
                    o.OrderId == "ORD-FLOW2" && o.OperationType == InventoryDomain.Reserve);
            },
            timeout: TimeSpan.FromSeconds(30),
            description: "Inventory should receive a Reserve operation for ORD-FLOW2");
    }

    public async Task DisposeAsync()
    {
        _orchestrator?.Dispose();
        _inventory.Dispose();
        await Task.CompletedTask;
    }
}
