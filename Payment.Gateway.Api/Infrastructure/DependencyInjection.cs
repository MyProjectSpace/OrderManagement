using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Gateway.Api.Application.Abstractions;
using Payment.Gateway.Api.Infrastructure.Messaging;
using Payment.Gateway.Api.Infrastructure.Persistence;

namespace Payment.Gateway.Api.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PaymentGateway")
            ?? "Data Source=paymentgateway.db";

        services.AddDbContext<PaymentGatewayDbContext>(options => options.UseSqlite(connectionString));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IPaymentEventPublisher, MassTransitPaymentEventPublisher>();

        var useInMemory = configuration.GetValue("MessageBroker:UseInMemory", defaultValue: false);

        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<PaymentGatewayDbContext>(o =>
            {
                o.UseSqlite();
                o.UseBusOutbox();
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
