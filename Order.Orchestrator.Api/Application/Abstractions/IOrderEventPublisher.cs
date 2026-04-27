namespace Order.Orchestrator.Application.Abstractions;

public interface IOrderEventPublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken) where T : class;
    Task SendAsync<T>(T message, CancellationToken cancellationToken) where T : class;
}
