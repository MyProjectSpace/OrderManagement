using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Order.Orchestrator.Application.Abstractions;
using Order.Orchestrator.Infrastructure.Consumers;
using Order.Orchestrator.Infrastructure.Http;
using Order.Orchestrator.Infrastructure.Messaging;
using Order.Orchestrator.Infrastructure.Persistence;

namespace Order.Orchestrator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrchestratorInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Orchestrator")
            ?? "Data Source=orchestrator.db";

        services.AddDbContext<OrchestratorDbContext>(options => options.UseSqlite(connectionString));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ICorrelationContext, CorrelationContext>();
        services.AddScoped<IIdempotencyStore, EfIdempotencyStore>();
        services.AddScoped<IOrderEventPublisher, MassTransitOrderEventPublisher>();

        var omsBaseUrl = configuration["Services:Oms:BaseUrl"] ?? "http://localhost:5001";
        var inventoryBaseUrl = configuration["Services:Inventory:BaseUrl"] ?? "http://localhost:5003";

        services.AddHttpClient<IOmsClient, OmsHttpClient>(c =>
        {
            c.BaseAddress = new Uri(omsBaseUrl);
            c.Timeout = TimeSpan.FromSeconds(10);
        }).AddStandardResilienceHandler();

        services.AddHttpClient<IInventoryClient, InventoryHttpClient>(c =>
        {
            c.BaseAddress = new Uri(inventoryBaseUrl);
            c.Timeout = TimeSpan.FromSeconds(10);
        }).AddStandardResilienceHandler();

        var useInMemory = configuration.GetValue("MessageBroker:UseInMemory", defaultValue: false);

        services.AddMassTransit(x =>
        {
            x.AddConsumer<PendingSyncRequestedConsumer, PendingSyncRequestedConsumerDefinition>();
            x.AddConsumer<AllocateOrderRequestedConsumer, AllocateOrderRequestedConsumerDefinition>();
            x.AddConsumer<PaymentConfirmedConsumer, PaymentConfirmedConsumerDefinition>();

            x.AddEntityFrameworkOutbox<OrchestratorDbContext>(o =>
            {
                o.UseSqlite();
                o.UseBusOutbox();
                o.QueryDelay = TimeSpan.FromSeconds(5);
                o.QueryMessageLimit = 50;
                o.QueryTimeout = TimeSpan.FromSeconds(30);
            });

            if (useInMemory)
            {
                x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
            }
            else
            {
                x.UsingRabbitMq((ctx, cfg) =>
                {
                    var host = configuration["RabbitMq:Host"] ?? "localhost";
                    var user = configuration["RabbitMq:Username"] ?? "guest";
                    var pass = configuration["RabbitMq:Password"] ?? "guest";
                    cfg.Host(host, h =>
                    {
                        h.Username(user);
                        h.Password(pass);
                    });
                    cfg.ConfigureEndpoints(ctx);
                });
            }
        });

        return services;
    }
}
