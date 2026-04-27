namespace Order.Orchestrator.Tests.Integration.Infrastructure;

/// <summary>
/// Forces all tests tagged with [Collection("Integration")] to run
/// sequentially. The integration/stress tests boot multiple SQLite :memory:
/// connections and MassTransit InMemory buses; running them in parallel
/// causes IServiceProvider/DbContext disposal races inside MassTransit's
/// EF outbox query loop.
/// </summary>
[CollectionDefinition("Integration", DisableParallelization = true)]
public class IntegrationCollection;
