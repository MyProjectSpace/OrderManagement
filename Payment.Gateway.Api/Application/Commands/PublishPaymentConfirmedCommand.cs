using MediatR;

namespace Payment.Gateway.Api.Application.Commands;

public record PublishPaymentConfirmedCommand(
    string OrderId,
    string CustomerId,
    string[] Items,
    decimal Total,
    DateTime? PaidAt = null) : IRequest<Unit>;
