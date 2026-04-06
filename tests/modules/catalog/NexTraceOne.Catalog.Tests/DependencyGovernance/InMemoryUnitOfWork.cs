using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.Catalog.Tests.DependencyGovernance;

internal sealed class InMemoryUnitOfWork : IUnitOfWork
{
    public int CommitCount { get; private set; }

    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        CommitCount++;
        return Task.FromResult(CommitCount);
    }
}
