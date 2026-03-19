using System.Collections.Concurrent;

namespace NexTraceOne.BackgroundWorkers;

public sealed class WorkerJobHealthRegistry
{
    private readonly ConcurrentDictionary<string, WorkerJobHealthSnapshot> _jobs = new(StringComparer.OrdinalIgnoreCase);

    public void MarkStarted(string jobName)
    {
        _jobs.AddOrUpdate(
            jobName,
            _ => new WorkerJobHealthSnapshot(jobName, DateTimeOffset.UtcNow, null, null, null, true),
            (_, current) => current with
            {
                LastStartedAt = DateTimeOffset.UtcNow,
                IsRunning = true
            });
    }

    public void MarkSucceeded(string jobName)
    {
        _jobs.AddOrUpdate(
            jobName,
            _ => new WorkerJobHealthSnapshot(jobName, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null, false),
            (_, current) => current with
            {
                LastSuccessAt = DateTimeOffset.UtcNow,
                LastErrorAt = null,
                LastError = null,
                IsRunning = false
            });
    }

    public void MarkFailed(string jobName, string error)
    {
        _jobs.AddOrUpdate(
            jobName,
            _ => new WorkerJobHealthSnapshot(jobName, DateTimeOffset.UtcNow, null, DateTimeOffset.UtcNow, error, false),
            (_, current) => current with
            {
                LastErrorAt = DateTimeOffset.UtcNow,
                LastError = error,
                IsRunning = false
            });
    }

    internal WorkerJobHealthSnapshot? GetSnapshot(string jobName)
        => _jobs.TryGetValue(jobName, out var snapshot) ? snapshot : null;
}

internal sealed record WorkerJobHealthSnapshot(
    string JobName,
    DateTimeOffset? LastStartedAt,
    DateTimeOffset? LastSuccessAt,
    DateTimeOffset? LastErrorAt,
    string? LastError,
    bool IsRunning);
