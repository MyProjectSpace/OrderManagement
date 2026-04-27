using MassTransit;
using Payment.Gateway.Api.Application.Abstractions;
using Payment.Gateway.Api.Infrastructure.Persistence;
using Shared.Contracts;

namespace Payment.Gateway.Api.Infrastructure.Messaging;

public class MassTransitPaymentEventPublisher(
    IPublishEndpoint publishEndpoint,
    PaymentGatewayDbContext db) : IPaymentEventPublisher
{
    public async Task PublishAsync(PaymentConfirmedEvent paymentConfirmedEvent, CancellationToken cancellationToken)
    {
        await publishEndpoint.Publish(paymentConfirmedEvent, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}
