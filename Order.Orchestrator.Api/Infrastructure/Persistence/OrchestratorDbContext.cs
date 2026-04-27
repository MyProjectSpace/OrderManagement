using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.Orchestrator.Domain;

namespace Order.Orchestrator.Infrastructure.Persistence;

public class OrchestratorDbContext(DbContextOptions<OrchestratorDbContext> options) : DbContext(options)
{
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessedMessage>(b =>
        {
            b.ToTable("ProcessedMessages", t =>
                t.HasCheckConstraint("CK_ProcessedMessages_OperationType",
                    "OperationType IN ('Allocate','Reserve')"));
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.CorrelationId).HasMaxLength(64);
            b.Property(x => x.OrderId).HasMaxLength(64);
            b.Property(x => x.OperationType).HasConversion<string>().HasMaxLength(16);
            b.Property(x => x.ProcessedAtUtc);
            b.HasIndex(x => new { x.OrderId, x.OperationType }).IsUnique();
        });

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
