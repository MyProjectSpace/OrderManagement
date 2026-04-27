using MediatR;
using Payment.Gateway.Api.Application.Commands;

namespace Payment.Gateway.Api.Endpoints;

public record PaymentConfirmedRequest(string OrderId, string CustomerId, string[] Items, decimal Total, DateTime? PaidAt);

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/payment-confirmed", async (PaymentConfirmedRequest request, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new PublishPaymentConfirmedCommand(
                request.OrderId, request.CustomerId, request.Items, request.Total, request.PaidAt), ct);
            return Results.Accepted();
        });

        return app;
    }
}
