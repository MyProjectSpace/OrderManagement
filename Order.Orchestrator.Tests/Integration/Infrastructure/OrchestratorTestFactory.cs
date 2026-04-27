extern alias OrchestratorApi;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Order.Orchestrator.Infrastructure.Persistence;

namespace Order.Orchestrator.Tests.Integration.Infrastructure;

public class OrchestratorTestFactory : WebApplicationFactory<OrchestratorApi::Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private string? _omsBaseAddress;
    private string? _inventoryBaseAddress;
    private Action<IServiceCollection>? _serviceOverrides;

    public OrchestratorTestFactory WithDownstreamUrls(string? omsBaseAddress, string? inventoryBaseAddress)
    {
        _omsBaseAddress = omsBaseAddress;
        _inventoryBaseAddress = inventoryBaseAddress;
        return this;
    }

    public OrchestratorTestFactory WithServices(Action<IServiceCollection> overrides)
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
            var settings = new Dictionary<string, string?>
            {
                ["MessageBroker:UseInMemory"] = "true",
                ["ConnectionStrings:Orchestrator"] = "DataSource=:memory:"
            };
            if (_omsBaseAddress is not null)
                settings["Services:Oms:BaseUrl"] = _omsBaseAddress;
            if (_inventoryBaseAddress is not null)
                settings["Services:Inventory:BaseUrl"] = _inventoryBaseAddress;

            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            ReplaceDbContext(services);
            _serviceOverrides?.Invoke(services);
        });
    }

    private void ReplaceDbContext(IServiceCollection services)
    {
        services.RemoveAll<DbContextOptions<OrchestratorDbContext>>();
        services.RemoveAll<OrchestratorDbContext>();

        services.AddDbContext<OrchestratorDbContext>(options =>
        {
            options.UseSqlite(_connection);
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
