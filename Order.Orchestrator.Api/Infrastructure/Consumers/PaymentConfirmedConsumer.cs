using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Order.Orchestrator.Application.Commands;
using Shared.Contracts;

namespace Order.Orchestrator.Infrastructure.Consumers;

public class PaymentConfirmedConsumer(
    ISender mediator,
    ILogger<PaymentConfirmedConsumer> logger) : IConsumer<PaymentConfirmedEvent>
{
    public async Task Consume(ConsumeContext<PaymentConfirmedEvent> context)
    {
        var msg = context.Message;
        var correlationId = context.Headers.Get<string>("X-Correlation-Id") ?? context.CorrelationId?.ToString();
        logger.LogInformation("Consuming PaymentConfirmedEvent for order {OrderId}", msg.OrderId);
        await mediator.Send(
            new ReserveInventoryCommand(msg.OrderId, msg.CustomerId, msg.Items, correlationId),
            context.CancellationToken);
    }
}

public class PaymentConfirmedConsumerDefinition : ConsumerDefinition<PaymentConfirmedConsumer>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<PaymentConfirmedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Exponential(3, TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(200));
            r.Ignore<ValidationException>();
        });
        endpointConfigurator.PrefetchCount = 32;
        endpointConfigurator.ConcurrentMessageLimit = 8;
    }
}

public class PendingSyncRequestedConsumerDefinition : ConsumerDefinition<PendingSyncRequestedConsumer>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<PendingSyncRequestedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Exponential(3, TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(200));
            r.Ignore<ValidationException>();
        });
        endpointConfigurator.PrefetchCount = 16;
        endpointConfigurator.ConcurrentMessageLimit = 4;
    }
}
