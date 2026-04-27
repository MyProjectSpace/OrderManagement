namespace Order.Orchestrator.Tests.Integration.Infrastructure;

internal static class TestPolling
{
    public static async Task UntilAsync(
        Func<Task<bool>> condition,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        string? description = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(100);

        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
                return;
            await Task.Delay(interval);
        }

        if (await condition())
            return;

        throw new TimeoutException(
            $"Condition '{description ?? "unspecified"}' was not satisfied within {timeout ?? TimeSpan.FromSeconds(30)}.");
    }
}
