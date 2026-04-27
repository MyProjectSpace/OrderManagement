using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Order.Orchestrator.Application.Commands;
using Order.Orchestrator.Infrastructure.Consumers;
using Shared.Contracts;

namespace Order.Orchestrator.Tests.Consumers;

public class PendingSyncRequestedConsumerTests
{
    [Fact]
    public async Task ConsumesMessage_DispatchesSyncCommand()
    {
        var mediator = new Mock<ISender>();
        SyncPendingOrdersCommand? captured = null;
        mediator
            .Setup(m => m.Send(It.IsAny<SyncPendingOrdersCommand>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((req, _) => captured = (SyncPendingOrdersCommand)req)
            .ReturnsAsync(0);

        await using var provider = new ServiceCollection()
            .AddSingleton(mediator.Object)
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<PendingSyncRequestedConsumer, PendingSyncRequestedConsumerDefinition>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        await harness.Bus.Publish(new PendingSyncRequested("corr-consumer-test"));

        (await harness.Consumed.Any<PendingSyncRequested>()).Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().Be("corr-consumer-test");
    }
}
