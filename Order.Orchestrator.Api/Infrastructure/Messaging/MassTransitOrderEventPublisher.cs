using MassTransit;
using Order.Orchestrator.Application.Abstractions;
using Order.Orchestrator.Infrastructure.Persistence;
using Shared.Contracts;

namespace Order.Orchestrator.Infrastructure.Messaging;

public class MassTransitOrderEventPublisher(
    IPublishEndpoint publishEndpoint,
    ISendEndpointProvider sendProvider,
    OrchestratorDbContext db) : IOrderEventPublisher
{
    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken) where T : class
    {
        await publishEndpoint.Publish(message, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SendAsync<T>(T message, CancellationToken cancellationToken) where T : class
    {
        var endpointName = message switch
        {
            AllocateOrderRequested => "allocate-order",
            _ => typeof(T).Name.ToLowerInvariant()
        };
        var endpoint = await sendProvider.GetSendEndpoint(new Uri($"queue:{endpointName}"));
        await endpoint.Send(message, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}
