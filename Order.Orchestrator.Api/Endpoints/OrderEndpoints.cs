using MediatR;
using Order.Orchestrator.Application.Commands;
using Shared.Contracts;

namespace Order.Orchestrator.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/orders/pending-ping", async (
            PendingPingRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new EnqueuePendingSyncCommand(request.CorrelationId), cancellationToken);
            return Results.Accepted(value: result);
        });

        return app;
    }
}
