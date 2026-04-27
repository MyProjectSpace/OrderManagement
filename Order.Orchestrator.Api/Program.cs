using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Order.Orchestrator.Api.Endpoints;
using Order.Orchestrator.Api.ExceptionHandling;
using Order.Orchestrator.Api.Middleware;
using Order.Orchestrator.Application;
using Order.Orchestrator.Infrastructure;
using Order.Orchestrator.Infrastructure.Persistence;
using Serilog;

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
        .Enrich.WithProperty("Service", "Order.Orchestrator.Api"));

    builder.Services.AddOrchestratorApplication();
    builder.Services.AddOrchestratorInfrastructure(builder.Configuration);
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    var connectionString = builder.Configuration.GetConnectionString("Orchestrator") ?? "Data Source=orchestrator.db";
    builder.Services.AddHealthChecks()
        .AddSqlite(connectionString, name: "sqlite", tags: ["ready"]);

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();
    app.UseMiddleware<CorrelationIdMiddleware>();

    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = c => c.Tags.Contains("ready"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapOrderEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Order.Orchestrator.Api terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
