namespace Payment.Gateway.Api.Domain;

public record PaymentConfirmation(
    string OrderId,
    string CustomerId,
    string[] Items,
    decimal Total,
    DateTime PaidAtUtc);
