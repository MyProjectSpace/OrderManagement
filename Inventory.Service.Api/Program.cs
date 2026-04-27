using FluentValidation;
using HealthChecks.UI.Client;
using Inventory.Service.Api.Endpoints;
using Inventory.Service.Api.ExceptionHandling;
using Inventory.Service.Application;
using Inventory.Service.Application.Commands;
using Inventory.Service.Domain;
using Inventory.Service.Infrastructure;
using Inventory.Service.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Shared.Contracts;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console()
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "Inventory.Service"));

    builder.Services.AddInventoryApplication();
    builder.Services.AddInventoryInfrastructure(builder.Configuration);
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<DomainExceptionHandler>();

    var connectionString = builder.Configuration.GetConnectionString("Inventory") ?? "Data Source=inventory.db";
    builder.Services.AddHealthChecks()
        .AddSqlite(connectionString, name: "sqlite", tags: ["ready"]);

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();

    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = c => c.Tags.Contains("ready"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapInventoryEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Inventory.Service.Api terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
