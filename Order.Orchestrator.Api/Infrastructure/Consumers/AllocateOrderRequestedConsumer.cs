using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Order.Orchestrator.Application.Commands;
using Order.Orchestrator.Infrastructure.Http;
using Shared.Contracts;

namespace Order.Orchestrator.Infrastructure.Consumers;

public class AllocateOrderRequestedConsumer(
    ISender mediator,
    ILogger<AllocateOrderRequestedConsumer> logger) : IConsumer<AllocateOrderRequested>
{
    public async Task Consume(ConsumeContext<AllocateOrderRequested> context)
    {
        var msg = context.Message;
        logger.LogInformation("Consuming AllocateOrderRequested for order {OrderId}", msg.OrderId);
        await mediator.Send(
            new AllocateOrderCommand(msg.OrderId, msg.CustomerId, msg.Items, msg.CorrelationId),
            context.CancellationToken);
    }
}

public class AllocateOrderRequestedConsumerDefinition : ConsumerDefinition<AllocateOrderRequestedConsumer>
{
    public AllocateOrderRequestedConsumerDefinition()
    {
        EndpointName = "allocate-order";
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<AllocateOrderRequestedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Exponential(3, TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(200));
            r.Ignore<ValidationException>();
            r.Ignore<InventoryClientPoisonException>();
        });
        endpointConfigurator.PrefetchCount = 32;
        endpointConfigurator.ConcurrentMessageLimit = 8;
    }
}
