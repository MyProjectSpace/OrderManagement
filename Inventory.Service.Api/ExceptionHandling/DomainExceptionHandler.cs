using System.Diagnostics;
using FluentValidation;
using Inventory.Service.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Service.Api.ExceptionHandling;

public sealed class DomainExceptionHandler(ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            UnknownSkuException => (StatusCodes.Status404NotFound, "Unknown SKU"),
            InsufficientStockException => (StatusCodes.Status409Conflict, "Insufficient stock"),
            InvalidInventoryOperationException => (StatusCodes.Status400BadRequest, "Invalid inventory operation"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            _ => (0, string.Empty)
        };

        if (status == 0)
        {
            return false;
        }

        logger.LogWarning(exception, "Handled domain exception {ExceptionType}", exception.GetType().Name);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception.Message,
            Type = $"https://httpstatuses.io/{status}",
            Instance = httpContext.Request.Path
        };
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
