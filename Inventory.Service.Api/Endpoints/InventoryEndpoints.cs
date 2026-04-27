using Inventory.Service.Application.Commands;
using Inventory.Service.Domain;
using MediatR;
using Shared.Contracts;

namespace Inventory.Service.Api.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/inventory/allocate", async (
            InventoryAllocationRequest request,
            HttpContext httpContext,
            ISender mediator,
            CancellationToken ct) =>
        {
            var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault();
            var result = await mediator.Send(
                new ApplyInventoryCommand(request.OrderId, request.Items, OperationType.Allocate, correlationId),
                ct);
            return Results.Ok(result);
        });

        app.MapPost("/inventory/reserve", async (
            InventoryAllocationRequest request,
            HttpContext httpContext,
            ISender mediator,
            CancellationToken ct) =>
        {
            var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault();
            var result = await mediator.Send(
                new ApplyInventoryCommand(request.OrderId, request.Items, OperationType.Reserve, correlationId),
                ct);
            return Results.Ok(result);
        });

        return app;
    }
}
