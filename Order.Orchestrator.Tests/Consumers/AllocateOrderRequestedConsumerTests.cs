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

public class AllocateOrderRequestedConsumerTests
{
    [Fact]
    public async Task ConsumesMessage_DispatchesAllocateCommand()
    {
        var mediator = new Mock<ISender>();
        AllocateOrderCommand? captured = null;
        mediator
            .Setup(m => m.Send(It.IsAny<AllocateOrderCommand>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((req, _) => captured = (AllocateOrderCommand)req)
            .ReturnsAsync(new AllocateOrderResult("ORD-77", true, false));

        await using var provider = new ServiceCollection()
            .AddSingleton(mediator.Object)
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<AllocateOrderRequestedConsumer, AllocateOrderRequestedConsumerDefinition>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        // Sent to the named "allocate-order" endpoint, matching the
        // orchestrator's IOrderEventPublisher.SendAsync routing.
        var endpoint = await harness.Bus.GetSendEndpoint(new Uri("queue:allocate-order"));
        await endpoint.Send(new AllocateOrderRequested("corr-1", "ORD-77", "CUST-77", ["ITEM-A"]));

        (await harness.Consumed.Any<AllocateOrderRequested>()).Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.OrderId.Should().Be("ORD-77");
        captured.CorrelationId.Should().Be("corr-1");
    }
}
