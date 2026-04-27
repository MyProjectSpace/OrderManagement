using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Order.Orchestrator.Application.Abstractions;
using Order.Orchestrator.Application.Commands;
using Shared.Contracts;

namespace Order.Orchestrator.Tests.Unit;

public class EnqueuePendingSyncHandlerTests
{
    [Fact]
    public async Task PublishesPendingSyncRequested_WithCorrelationId()
    {
        var publisher = new Mock<IOrderEventPublisher>();
        PendingSyncRequested? captured = null;
        publisher
            .Setup(p => p.PublishAsync(It.IsAny<PendingSyncRequested>(), It.IsAny<CancellationToken>()))
            .Callback<PendingSyncRequested, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);

        var handler = new EnqueuePendingSyncCommandHandler(
            publisher.Object,
            NullLogger<EnqueuePendingSyncCommandHandler>.Instance);

        var correlationId = $"enq-publish-{Guid.NewGuid():N}";
        var result = await handler.Handle(
            new EnqueuePendingSyncCommand(correlationId),
            CancellationToken.None);

        result.CorrelationId.Should().Be(correlationId);
        result.Coalesced.Should().BeFalse();
        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public async Task ConcurrentPings_SameCorrelation_AreCoalesced()
    {
        // Spec/CLAUDE convention: while one ping is in flight, a second with
        // the same correlation id collapses to Coalesced=true and does NOT
        // publish a second message.
        var publisher = new Mock<IOrderEventPublisher>();
        var releaseFirst = new TaskCompletionSource();

        publisher
            .Setup(p => p.PublishAsync(It.IsAny<PendingSyncRequested>(), It.IsAny<CancellationToken>()))
            .Returns(async () => await releaseFirst.Task);

        var handler = new EnqueuePendingSyncCommandHandler(
            publisher.Object,
            NullLogger<EnqueuePendingSyncCommandHandler>.Instance);

        var correlationId = $"enq-coalesce-{Guid.NewGuid():N}";

        var firstTask = handler.Handle(new EnqueuePendingSyncCommand(correlationId), CancellationToken.None);

        // Spin until the first publish has been entered.
        await TestUtils.WaitForAsync(
            () => publisher.Invocations.Count(i => i.Method.Name == nameof(IOrderEventPublisher.PublishAsync)) >= 1);

        var secondResult = await handler.Handle(
            new EnqueuePendingSyncCommand(correlationId),
            CancellationToken.None);

        secondResult.Coalesced.Should().BeTrue();

        releaseFirst.SetResult();
        var firstResult = await firstTask;
        firstResult.Coalesced.Should().BeFalse();

        publisher.Verify(
            p => p.PublishAsync(It.IsAny<PendingSyncRequested>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

internal static class TestUtils
{
    public static async Task WaitForAsync(Func<bool> condition, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (condition()) return;
            await Task.Delay(10);
        }
        throw new TimeoutException("Condition not satisfied within timeout.");
    }
}
