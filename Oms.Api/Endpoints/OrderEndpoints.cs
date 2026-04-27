using MediatR;
using Oms.Api.Application.Commands;
using Oms.Api.Application.Queries;

namespace Oms.Api.Endpoints;

public record TriggerPendingPingRequest(string? CorrelationId);

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/orders/pending", async (ISender sender, CancellationToken ct) =>
        {
            var orders = await sender.Send(new GetPendingOrdersQuery(), ct);
            return Results.Ok(orders);
        });

        app.MapPost("/trigger-pending-ping", async (
            TriggerPendingPingRequest? request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new TriggerPendingPingCommand(request?.CorrelationId), ct);
            return Results.Accepted(value: result);
        });

        return app;
    }
}
