using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Oms.Api.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddOmsApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        return services;
    }
}
