using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Gateway.Api.Application.Abstractions;
using Shared.Contracts;

namespace Payment.Gateway.Api.Application.Commands;

public class PublishPaymentConfirmedCommandHandler(
    IPaymentEventPublisher publisher,
    TimeProvider clock,
    ILogger<PublishPaymentConfirmedCommandHandler> logger)
    : IRequestHandler<PublishPaymentConfirmedCommand, Unit>
{
    public async Task<Unit> Handle(PublishPaymentConfirmedCommand request, CancellationToken ct)
    {
        var paidAt = request.PaidAt ?? clock.GetUtcNow().UtcDateTime;
        var evt = new PaymentConfirmedEvent(request.OrderId, request.CustomerId, request.Items, request.Total, paidAt);

        await publisher.PublishAsync(evt, ct);
        logger.LogInformation("Published PaymentConfirmedEvent for order {OrderId}", request.OrderId);
        return Unit.Value;
    }
}
