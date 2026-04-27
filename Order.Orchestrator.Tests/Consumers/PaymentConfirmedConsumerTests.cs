using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Order.Orchestrator.Application.Commands;
using Order.Orchestrator.Infrastructure.Consumers;
using Order.Orchestrator.Infrastructure.Http;
using Shared.Contracts;

namespace Order.Orchestrator.Tests.Consumers;

public class PaymentConfirmedConsumerTests
{
    [Fact]
    public async Task QueueProcessor_ProcessesOrder_Success()
    {
        var mediator = new Mock<ISender>();
        ReserveInventoryCommand? captured = null;
        mediator
            .Setup(m => m.Send(It.IsAny<ReserveInventoryCommand>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((req, _) => captured = (ReserveInventoryCommand)req)
            .ReturnsAsync(new ReserveInventoryResult("ORD-100", true, false));

        await using var provider = new ServiceCollection()
            .AddSingleton(mediator.Object)
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<PaymentConfirmedConsumer, PaymentConfirmedConsumerDefinition>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var evt = new PaymentConfirmedEvent("ORD-100", "CUST-100", ["ITEM-A"], 49.95m, DateTime.UtcNow);
        await harness.Bus.Publish(evt);

        (await harness.Consumed.Any<PaymentConfirmedEvent>()).Should().BeTrue();
        var consumerHarness = harness.GetConsumerHarness<PaymentConfirmedConsumer>();
        (await consumerHarness.Consumed.Any<PaymentConfirmedEvent>()).Should().BeTrue();

        captured.Should().NotBeNull();
        captured!.OrderId.Should().Be("ORD-100");
        captured.Items.Should().BeEquivalentTo(new[] { "ITEM-A" });
    }

    [Fact]
    public async Task QueueProcessor_Fails3Times_DeadLetters()
    {
        // ConsumerDefinition retries 3× with exponential backoff. After the
        // last failure MassTransit publishes a Fault<T> and routes the
        // original message to the consumer's _error queue.
        var attempts = 0;
        var mediator = new Mock<ISender>();
        mediator
            .Setup(m => m.Send(It.IsAny<ReserveInventoryCommand>(), It.IsAny<CancellationToken>()))
            .Callback(() => Interlocked.Increment(ref attempts))
            .ThrowsAsync(new HttpRequestException("inventory down"));

        await using var provider = new ServiceCollection()
            .AddSingleton(mediator.Object)
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<PaymentConfirmedConsumer, PaymentConfirmedConsumerDefinition>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        harness.TestTimeout = TimeSpan.FromSeconds(30);
        await harness.Start();

        var evt = new PaymentConfirmedEvent("ORD-FAIL", "CUST-FAIL", ["ITEM-A"], 1m, DateTime.UtcNow);
        await harness.Bus.Publish(evt);

        (await harness.Published.Any<Fault<PaymentConfirmedEvent>>()).Should().BeTrue(
            "after 3 retries the consumer should publish a Fault<T> and dead-letter");

        // Initial attempt + 3 retries = 4 invocations of the mediator.
        attempts.Should().Be(4);
    }

    [Fact]
    public async Task QueueProcessor_PoisonFailure_DeadLettersImmediately()
    {
        var attempts = 0;
        var mediator = new Mock<ISender>();
        mediator
            .Setup(m => m.Send(It.IsAny<ReserveInventoryCommand>(), It.IsAny<CancellationToken>()))
            .Callback(() => Interlocked.Increment(ref attempts))
            .ThrowsAsync(new InventoryClientPoisonException(404, "Unknown SKU", "ITEM-Z"));

        await using var provider = new ServiceCollection()
            .AddSingleton(mediator.Object)
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<PaymentConfirmedConsumer, PaymentConfirmedConsumerDefinition>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        harness.TestTimeout = TimeSpan.FromSeconds(30);
        await harness.Start();

        var evt = new PaymentConfirmedEvent("ORD-POISON", "CUST-1", ["ITEM-Z"], 1m, DateTime.UtcNow);
        await harness.Bus.Publish(evt);

        (await harness.Published.Any<Fault<PaymentConfirmedEvent>>()).Should().BeTrue(
            "poison failures must publish a Fault<T> on the first attempt");

        attempts.Should().Be(1, "poison failures must bypass the retry policy");
    }
}
