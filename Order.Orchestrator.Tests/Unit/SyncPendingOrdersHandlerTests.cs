using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Order.Orchestrator.Application.Abstractions;
using Order.Orchestrator.Application.Commands;
using Shared.Contracts;

namespace Order.Orchestrator.Tests.Unit;

public class SyncPendingOrdersHandlerTests
{
    [Fact]
    public async Task FetchesFromOms_AndFanOutsOnePerOrder()
    {
        var orders = new List<PendingOrder>
        {
            new("ORD-1", "CUST-1", ["ITEM-A"], 10m),
            new("ORD-2", "CUST-2", ["ITEM-B", "ITEM-C"], 20m),
            new("ORD-3", "CUST-3", ["ITEM-D"], 30m)
        };

        var oms = new Mock<IOmsClient>();
        oms.Setup(c => c.GetPendingOrdersAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(orders);

        var sent = new List<AllocateOrderRequested>();
        var publisher = new Mock<IOrderEventPublisher>();
        publisher
            .Setup(p => p.SendAsync(It.IsAny<AllocateOrderRequested>(), It.IsAny<CancellationToken>()))
            .Callback<AllocateOrderRequested, CancellationToken>((m, _) => sent.Add(m))
            .Returns(Task.CompletedTask);

        var handler = new SyncPendingOrdersCommandHandler(
            oms.Object, publisher.Object,
            NullLogger<SyncPendingOrdersCommandHandler>.Instance);

        var count = await handler.Handle(new SyncPendingOrdersCommand("corr-1"), CancellationToken.None);

        count.Should().Be(3);
        sent.Should().HaveCount(3);
        sent.Select(s => s.OrderId).Should().BeEquivalentTo(new[] { "ORD-1", "ORD-2", "ORD-3" });
        sent.Should().OnlyContain(s => s.CorrelationId == "corr-1");
    }

    [Fact]
    public async Task NoPendingOrders_ReturnsZero_AndPublishesNothing()
    {
        var oms = new Mock<IOmsClient>();
        oms.Setup(c => c.GetPendingOrdersAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new List<PendingOrder>());

        var publisher = new Mock<IOrderEventPublisher>();

        var handler = new SyncPendingOrdersCommandHandler(
            oms.Object, publisher.Object,
            NullLogger<SyncPendingOrdersCommandHandler>.Instance);

        var count = await handler.Handle(new SyncPendingOrdersCommand("corr-empty"), CancellationToken.None);

        count.Should().Be(0);
        publisher.Verify(
            p => p.SendAsync(It.IsAny<AllocateOrderRequested>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
