using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Oms.Api.Domain;

namespace Oms.Api.Infrastructure.Persistence;

public class OmsDbContext(DbContextOptions<OmsDbContext> options) : DbContext(options)
{
    public DbSet<PendingOrder> PendingOrders => Set<PendingOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PendingOrder>(b =>
        {
            b.ToTable("PendingOrders", t =>
                t.HasCheckConstraint("CK_PendingOrders_Status",
                    "Status IN ('Pending','Picked','Completed')"));

            b.HasKey(x => x.OrderId);
            b.Property(x => x.OrderId).HasMaxLength(64);
            b.Property(x => x.CustomerId).HasMaxLength(64);
            b.Property(x => x.Total).HasColumnType("NUMERIC");
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
            b.Property(x => x.CreatedAtUtc);

            b.Property(x => x.Items)
                .HasColumnName("ItemsJson")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<string>(),
                    new ValueComparer<string[]>(
                        (a, b) => a!.SequenceEqual(b!),
                        a => a.Aggregate(0, (h, x) => HashCode.Combine(h, x.GetHashCode())),
                        a => a.ToArray()));
        });
    }
}
