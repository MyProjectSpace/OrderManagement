using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Order.Orchestrator.Domain;
using Order.Orchestrator.Infrastructure.Persistence;

namespace Order.Orchestrator.Tests.Unit;

public class EfIdempotencyStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly OrchestratorDbContext _db;
    private readonly FakeTimeProvider _clock;

    public EfIdempotencyStoreTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<OrchestratorDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new OrchestratorDbContext(options);
        _db.Database.EnsureCreated();

        _clock = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task MarkProcessed_ThenHasProcessed_ReturnsTrue()
    {
        var store = new EfIdempotencyStore(_db, _clock);

        (await store.HasProcessedAsync("ORD-1", OperationType.Allocate, default)).Should().BeFalse();

        await store.MarkProcessedAsync("ORD-1", OperationType.Allocate, "corr-1", default);

        (await store.HasProcessedAsync("ORD-1", OperationType.Allocate, default)).Should().BeTrue();
        (await store.HasProcessedAsync("ORD-1", OperationType.Reserve, default)).Should().BeFalse(
            "the unique index is on (OrderId, OperationType), so Reserve is independent");
    }

    [Fact]
    public async Task DuplicateMark_DoesNotThrow_BecauseRowExists()
    {
        // The handler catches DbUpdateException when the unique index rejects
        // a duplicate, then re-checks and treats existing-row as success.
        var store = new EfIdempotencyStore(_db, _clock);

        await store.MarkProcessedAsync("ORD-2", OperationType.Reserve, "corr-2", default);

        var act = async () => await store.MarkProcessedAsync("ORD-2", OperationType.Reserve, "corr-2-dup", default);
        await act.Should().NotThrowAsync();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
