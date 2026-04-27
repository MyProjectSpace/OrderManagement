using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Payment.Gateway.Api.Application;
using Payment.Gateway.Api.Endpoints;
using Payment.Gateway.Api.ExceptionHandling;
using Payment.Gateway.Api.Infrastructure;
using Payment.Gateway.Api.Infrastructure.Persistence;
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
        .Enrich.WithProperty("Service", "Payment.Gateway.Api"));

    builder.Services.AddPaymentApplication();
    builder.Services.AddPaymentInfrastructure(builder.Configuration);
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

    var connectionString = builder.Configuration.GetConnectionString("PaymentGateway") ?? "Data Source=paymentgateway.db";
    builder.Services.AddHealthChecks()
        .AddSqlite(connectionString, name: "sqlite", tags: ["ready"]);

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<PaymentGatewayDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();

    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = c => c.Tags.Contains("ready"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapPaymentEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Payment.Gateway.Api terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
