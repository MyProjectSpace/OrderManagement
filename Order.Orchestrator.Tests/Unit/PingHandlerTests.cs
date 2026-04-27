using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Order.Orchestrator.Application.Abstractions;
using Order.Orchestrator.Application.Commands;
using Shared.Contracts;

namespace Order.Orchestrator.Tests.Unit;

public class PingHandlerTests
{
    [Fact]
    public async Task PingHandler_ReturnsImmediately()
    {
        // Spec: POST /orders/pending-ping returns 202 synchronously; the heavy
        // work (OMS pull + fan-out) runs asynchronously via the queue. The
        // handler proves this by enqueueing a single PendingSyncRequested
        // message and returning — it does NOT call IOmsClient inline.
        var publisher = new Mock<IOrderEventPublisher>();
        publisher
            .Setup(p => p.PublishAsync(It.IsAny<PendingSyncRequested>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new EnqueuePendingSyncCommandHandler(
            publisher.Object,
            NullLogger<EnqueuePendingSyncCommandHandler>.Instance);

        var sw = Stopwatch.StartNew();
        var result = await handler.Handle(
            new EnqueuePendingSyncCommand($"ping-immediate-{Guid.NewGuid():N}"),
            CancellationToken.None);
        sw.Stop();

        result.Coalesced.Should().BeFalse();
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(500),
            "the ping path must not block on downstream work");
        publisher.Verify(
            p => p.PublishAsync(It.IsAny<PendingSyncRequested>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
