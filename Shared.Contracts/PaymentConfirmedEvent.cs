namespace Shared.Contracts;

public record PaymentConfirmedEvent(
    string OrderId,
    string CustomerId,
    string[] Items,
    decimal Total,
    DateTime PaidAt);
