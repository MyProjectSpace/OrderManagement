using System.Runtime.CompilerServices;

namespace Order.Orchestrator.Tests.Integration.Infrastructure;

internal static class TestModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        // Production DI reads MassTransit transport choice eagerly during
        // `AddOrchestratorInfrastructure`/`AddPaymentInfrastructure`, before
        // WebApplicationFactory's ConfigureAppConfiguration runs. Setting the
        // env var here (loaded once at assembly initialization) makes the
        // setting visible to all factory builds.
        Environment.SetEnvironmentVariable("MessageBroker__UseInMemory", "true");
    }
}
