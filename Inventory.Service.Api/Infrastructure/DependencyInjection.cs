using Inventory.Service.Application.Abstractions;
using Inventory.Service.Infrastructure.Persistence;
using Inventory.Service.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Service.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Inventory")
            ?? "Data Source=inventory.db";

        services.AddDbContext<InventoryDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddSingleton(TimeProvider.System);
        services.AddHostedService<InventorySeeder>();

        return services;
    }
}
