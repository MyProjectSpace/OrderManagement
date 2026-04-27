using Inventory.Service.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Service.Infrastructure.Persistence;

public class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<InventoryItem> Items => Set<InventoryItem>();
    public DbSet<InventoryOperation> Operations => Set<InventoryOperation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>(b =>
        {
            b.ToTable("InventoryItems");
            b.HasKey(x => x.Sku);
            b.Property(x => x.Sku).HasMaxLength(64);
            b.Property(x => x.Available);
            b.Property(x => x.Reserved);
            b.Property(x => x.RowVersion)
            .IsConcurrencyToken()
            .HasDefaultValueSql("randomblob(8)")
            .ValueGeneratedOnAddOrUpdate();
        });

        modelBuilder.Entity<InventoryOperation>(b =>
        {
            b.ToTable("InventoryOperations", t =>
                t.HasCheckConstraint("CK_InventoryOperations_OperationType",
                    "OperationType IN ('Allocate','Reserve')"));
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.OrderId).HasMaxLength(64);
            b.Property(x => x.OperationType).HasConversion<string>().HasMaxLength(16);
            b.Property(x => x.Sku).HasMaxLength(64);
            b.Property(x => x.CorrelationId).HasMaxLength(64);
            b.HasIndex(x => new { x.OrderId, x.OperationType, x.Sku }).IsUnique();
        });
    }
}
