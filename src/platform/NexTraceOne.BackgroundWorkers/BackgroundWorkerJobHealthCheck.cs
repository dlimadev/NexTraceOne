using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NexTraceOne.BackgroundWorkers;

internal sealed class BackgroundWorkerJobHealthCheck(
    WorkerJobHealthRegistry registry,
    string jobName,
    TimeSpan maxSuccessAge) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var snapshot = registry.GetSnapshot(jobName);
        if (snapshot is null)
        {
            return Task.FromResult(new HealthCheckResult(
                context.Registration.FailureStatus,
                $"Background job '{jobName}' has not started yet."));
        }

        var now = DateTimeOffset.UtcNow;
        var data = new Dictionary<string, object?>
        {
            ["jobName"] = snapshot.JobName,
            ["isRunning"] = snapshot.IsRunning,
            ["lastStartedAtUtc"] = snapshot.LastStartedAt,
            ["lastSuccessAtUtc"] = snapshot.LastSuccessAt,
            ["lastErrorAtUtc"] = snapshot.LastErrorAt,
            ["lastError"] = snapshot.LastError
        };

        if (snapshot.LastSuccessAt is null)
        {
            var hasRecentStart = snapshot.LastStartedAt is not null && now - snapshot.LastStartedAt <= maxSuccessAge;
            return Task.FromResult(hasRecentStart
                ? HealthCheckResult.Healthy($"Background job '{jobName}' started and is awaiting its first successful cycle.", data)
                : new HealthCheckResult(context.Registration.FailureStatus, $"Background job '{jobName}' has not completed a successful cycle yet.", data: data));
        }

        var successAge = now - snapshot.LastSuccessAt.Value;
        data["secondsSinceLastSuccess"] = (long)successAge.TotalSeconds;

        if (successAge > maxSuccessAge)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Background job '{jobName}' is overdue for a successful cycle.",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Background job '{jobName}' is operating within the expected interval.",
            data));
    }
}
