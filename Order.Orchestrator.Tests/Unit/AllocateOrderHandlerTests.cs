using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Order.Orchestrator.Application.Abstractions;
using Order.Orchestrator.Application.Commands;
using Order.Orchestrator.Domain;
using Shared.Contracts;

namespace Order.Orchestrator.Tests.Unit;

public class AllocateOrderHandlerTests
{
    [Fact]
    public async Task NotProcessed_CallsInventory_AndMarksProcessed()
    {
        var inventory = new Mock<IInventoryClient>();
        inventory
            .Setup(c => c.AllocateAsync("ORD-1", It.IsAny<string[]>(), "corr-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryOperationResult("ORD-1", "Allocate", true));

        var idempotency = new Mock<IIdempotencyStore>();
        idempotency
            .Setup(s => s.HasProcessedAsync("ORD-1", OperationType.Allocate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new AllocateOrderCommandHandler(
            inventory.Object, idempotency.Object,
            NullLogger<AllocateOrderCommandHandler>.Instance);

        var result = await handler.Handle(
            new AllocateOrderCommand("ORD-1", "CUST-1", ["ITEM-A"], "corr-1"),
            CancellationToken.None);

        result.Accepted.Should().BeTrue();
        result.AlreadyProcessed.Should().BeFalse();
        inventory.Verify(c => c.AllocateAsync("ORD-1", It.IsAny<string[]>(), "corr-1", It.IsAny<CancellationToken>()), Times.Once);
        idempotency.Verify(s => s.MarkProcessedAsync("ORD-1", OperationType.Allocate, "corr-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AlreadyProcessed_SkipsInventoryCall()
    {
        var inventory = new Mock<IInventoryClient>(MockBehavior.Strict);

        var idempotency = new Mock<IIdempotencyStore>();
        idempotency
            .Setup(s => s.HasProcessedAsync("ORD-2", OperationType.Allocate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new AllocateOrderCommandHandler(
            inventory.Object, idempotency.Object,
            NullLogger<AllocateOrderCommandHandler>.Instance);

        var result = await handler.Handle(
            new AllocateOrderCommand("ORD-2", "CUST-2", ["ITEM-B"], "corr-2"),
            CancellationToken.None);

        result.AlreadyProcessed.Should().BeTrue();
        result.Accepted.Should().BeTrue();
        idempotency.Verify(
            s => s.MarkProcessedAsync(It.IsAny<string>(), It.IsAny<OperationType>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
