using Shared.Contracts;

namespace Payment.Gateway.Api.Application.Abstractions;

public interface IPaymentEventPublisher
{
    Task PublishAsync(PaymentConfirmedEvent paymentConfirmedEvent, CancellationToken cancellationToken);
}
