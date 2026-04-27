using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Payment.Gateway.Api.Application.Behaviors;

namespace Payment.Gateway.Api.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);
        return services;
    }
}
