using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Order.Orchestrator.Application.Behaviors;

namespace Order.Orchestrator.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddOrchestratorApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);
        return services;
    }
}
