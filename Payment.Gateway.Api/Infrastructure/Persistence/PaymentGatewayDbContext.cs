using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Payment.Gateway.Api.Infrastructure.Persistence;

public class PaymentGatewayDbContext(DbContextOptions<PaymentGatewayDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
