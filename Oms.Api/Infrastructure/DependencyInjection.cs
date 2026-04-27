using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oms.Api.Application.Abstractions;
using Oms.Api.Infrastructure.Http;
using Oms.Api.Infrastructure.Persistence;
using Oms.Api.Infrastructure.Seeding;

namespace Oms.Api.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOmsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Oms")
            ?? "Data Source=oms.db";

        services.AddDbContext<OmsDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IPendingOrderRepository, PendingOrderRepository>();
        services.AddSingleton(TimeProvider.System);
        services.AddHostedService<OmsSeeder>();

        var orchestratorBaseUrl = configuration["Services:Orchestrator:BaseUrl"]
            ?? "http://localhost:5002";

        services.AddHttpClient<IOrchestratorClient, OrchestratorHttpClient>(c =>
        {
            c.BaseAddress = new Uri(orchestratorBaseUrl);
            c.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }
}
