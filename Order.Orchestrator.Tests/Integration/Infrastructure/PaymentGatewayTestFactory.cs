extern alias PaymentGatewayApi;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Payment.Gateway.Api.Infrastructure.Persistence;

namespace Order.Orchestrator.Tests.Integration.Infrastructure;

public class PaymentGatewayTestFactory : WebApplicationFactory<PaymentGatewayApi::Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private Action<IServiceCollection>? _serviceOverrides;

    public PaymentGatewayTestFactory WithServices(Action<IServiceCollection> overrides)
    {
        _serviceOverrides = overrides;
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MessageBroker:UseInMemory"] = "true",
                ["ConnectionStrings:PaymentGateway"] = "DataSource=:memory:"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PaymentGatewayDbContext>>();
            services.RemoveAll<PaymentGatewayDbContext>();

            services.AddDbContext<PaymentGatewayDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            _serviceOverrides?.Invoke(services);
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }
        base.Dispose(disposing);
    }
}
