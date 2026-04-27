using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Order.Orchestrator.Application.Abstractions;
using Order.Orchestrator.Application.Commands;
using Order.Orchestrator.Domain;
using Shared.Contracts;

namespace Order.Orchestrator.Tests.Unit;

public class ReserveInventoryHandlerTests
{
    [Fact]
    public async Task NotProcessed_CallsInventory_AndMarksProcessed()
    {
        var inventory = new Mock<IInventoryClient>();
        inventory
            .Setup(c => c.ReserveAsync("ORD-A", It.IsAny<string[]>(), "corr-A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryOperationResult("ORD-A", "Reserve", true));

        var idempotency = new Mock<IIdempotencyStore>();
        idempotency
            .Setup(s => s.HasProcessedAsync("ORD-A", OperationType.Reserve, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new ReserveInventoryCommandHandler(
            inventory.Object, idempotency.Object,
            NullLogger<ReserveInventoryCommandHandler>.Instance);

        var result = await handler.Handle(
            new ReserveInventoryCommand("ORD-A", "CUST-A", ["ITEM-F"], "corr-A"),
            CancellationToken.None);

        result.Accepted.Should().BeTrue();
        result.AlreadyProcessed.Should().BeFalse();
        inventory.Verify(c => c.ReserveAsync("ORD-A", It.IsAny<string[]>(), "corr-A", It.IsAny<CancellationToken>()), Times.Once);
        idempotency.Verify(s => s.MarkProcessedAsync("ORD-A", OperationType.Reserve, "corr-A", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AlreadyProcessed_SkipsInventoryCall()
    {
        var inventory = new Mock<IInventoryClient>(MockBehavior.Strict);

        var idempotency = new Mock<IIdempotencyStore>();
        idempotency
            .Setup(s => s.HasProcessedAsync("ORD-B", OperationType.Reserve, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new ReserveInventoryCommandHandler(
            inventory.Object, idempotency.Object,
            NullLogger<ReserveInventoryCommandHandler>.Instance);

        var result = await handler.Handle(
            new ReserveInventoryCommand("ORD-B", "CUST-B", ["ITEM-A"], "corr-B"),
            CancellationToken.None);

        result.AlreadyProcessed.Should().BeTrue();
        result.Accepted.Should().BeTrue();
        idempotency.Verify(
            s => s.MarkProcessedAsync(It.IsAny<string>(), It.IsAny<OperationType>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
