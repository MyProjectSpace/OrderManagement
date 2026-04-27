using Inventory.Service.Domain;
using Inventory.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Inventory.Service.Infrastructure.Seeding;

public class InventorySeeder(IServiceScopeFactory scopeFactory, ILogger<InventorySeeder> logger) : IHostedService
{
    private static readonly (string Sku, int Available, int Reserved)[] Seed =
    [
        ("ITEM-A", 100, 0),
        ("ITEM-B", 50, 0),
        ("ITEM-C", 25, 0),
        ("ITEM-D", 200, 0),
        ("ITEM-E", 10, 0),
        ("ITEM-F", 500, 0),
    ];

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            await db.Database.EnsureCreatedAsync(ct);
        if (await db.Items.AnyAsync(ct)) return;

        logger.LogInformation("Seeding {Count} inventory items", Seed.Length);
        db.Items.AddRange(Seed.Select(s => new InventoryItem(s.Sku, s.Available)));
        await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");    
        }
        
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
