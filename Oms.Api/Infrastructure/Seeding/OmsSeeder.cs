using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Oms.Api.Domain;
using Oms.Api.Infrastructure.Persistence;

namespace Oms.Api.Infrastructure.Seeding;

public class OmsSeeder(
    IServiceProvider serviceProvider,
    ILogger<OmsSeeder> logger,
    TimeProvider clock) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OmsDbContext>();

        await db.Database.EnsureCreatedAsync(cancellationToken);

        if (await db.PendingOrders.AnyAsync(cancellationToken))
        {
            logger.LogInformation("OMS already seeded");
            return;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var seed = new[]
        {
            new PendingOrder { OrderId = "ORD-1001", CustomerId = "CUST-1", Items = ["ITEM-A","ITEM-B"], Total = 49.95m, Status = OrderStatus.Pending, CreatedAtUtc = now },
            new PendingOrder { OrderId = "ORD-1002", CustomerId = "CUST-2", Items = ["ITEM-C"],         Total = 19.50m, Status = OrderStatus.Pending, CreatedAtUtc = now },
            new PendingOrder { OrderId = "ORD-1003", CustomerId = "CUST-3", Items = ["ITEM-D","ITEM-E"], Total = 75.00m, Status = OrderStatus.Pending, CreatedAtUtc = now },
            new PendingOrder { OrderId = "ORD-1004", CustomerId = "CUST-4", Items = ["ITEM-A"],         Total = 12.00m, Status = OrderStatus.Pending, CreatedAtUtc = now },
            new PendingOrder { OrderId = "ORD-1005", CustomerId = "CUST-5", Items = ["ITEM-B","ITEM-C"], Total = 33.25m, Status = OrderStatus.Pending, CreatedAtUtc = now },
            new PendingOrder { OrderId = "ORD-1006", CustomerId = "CUST-6", Items = ["ITEM-D"],         Total = 22.00m, Status = OrderStatus.Picked,    CreatedAtUtc = now },
            new PendingOrder { OrderId = "ORD-1007", CustomerId = "CUST-7", Items = ["ITEM-E"],         Total = 14.00m, Status = OrderStatus.Completed, CreatedAtUtc = now }
        };

        db.PendingOrders.AddRange(seed);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("OMS seeded {Count} orders", seed.Length);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
