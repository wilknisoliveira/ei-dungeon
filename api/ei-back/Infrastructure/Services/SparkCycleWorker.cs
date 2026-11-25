using ei_back.Core.Application.Service.Sparks;

namespace ei_back.Infrastructure.Services;

public sealed class SparkCycleWorker(
    IServiceScopeFactory scopes,
    TimeProvider clock,
    ILogger<SparkCycleWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run on minute boundaries so local midnight is reconciled before newly due queue work.
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunInScopeAsync<SparkCycleOperations>((operations, token) => operations.RunAsync(token), stoppingToken);
            await RunInScopeAsync<SparkNotifications>((notifications, token) => notifications.DispatchAsync(token), stoppingToken);
            var offset = clock.GetUtcNow().Ticks % TimeSpan.TicksPerMinute;
            await Task.Delay(TimeSpan.FromTicks(TimeSpan.TicksPerMinute - offset), clock, stoppingToken);
        }
    }

    private async Task RunInScopeAsync<T>(Func<T, CancellationToken, Task> run, CancellationToken stoppingToken) where T : notnull
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            await run(scope.ServiceProvider.GetRequiredService<T>(), stoppingToken);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Spark background operation {Operation} failed and will be retried.", typeof(T).Name);
        }
    }
}
