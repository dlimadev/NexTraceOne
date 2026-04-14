using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.DependencyGovernance.Abstractions;

namespace NexTraceOne.Catalog.Tests.DependencyGovernance;

internal sealed class InMemoryUnitOfWork : IDependencyGovernanceUnitOfWork
{
    public int CommitCount { get; private set; }

    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        CommitCount++;
        return Task.FromResult(CommitCount);
    }
}
