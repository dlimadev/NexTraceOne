using System.Net;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.BackgroundWorkers;
using NexTraceOne.BackgroundWorkers.Elasticsearch;
using NexTraceOne.BackgroundWorkers.Jobs;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Tests.Jobs;

public sealed class ElasticsearchIndexMaintenanceJobTests
{
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly WorkerJobHealthRegistry _registry = new();
    private readonly IElasticsearchIndexManager _indexManager = Substitute.For<IElasticsearchIndexManager>();

    private ElasticsearchIndexMaintenanceJob CreateJob()
    {
        var scope = Substitute.For<IServiceScope>();
        var provider = Substitute.For<IServiceProvider>();

        provider.GetService(typeof(IElasticsearchIndexManager)).Returns(_indexManager);
        scope.ServiceProvider.Returns(provider);
        _scopeFactory.CreateScope().Returns(scope);

        return new ElasticsearchIndexMaintenanceJob(
            _scopeFactory,
            _registry,
            NullLogger<ElasticsearchIndexMaintenanceJob>.Instance);
    }

    [Fact]
    public void HealthCheckName_IsCorrect()
    {
        ElasticsearchIndexMaintenanceJob.HealthCheckName.Should().Be("elasticsearch-index-maintenance-job");
    }

    [Fact]
    public async Task RunMaintenanceCycle_WhenClusterUnhealthy_SkipsPolicyApplication()
    {
        _indexManager.IsClusterHealthyAsync(Arg.Any<CancellationToken>()).Returns(false);

        var job = CreateJob();
        await job.RunMaintenanceCycleAsync(CancellationToken.None);

        await _indexManager.DidNotReceive().ApplyIlmPoliciesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunMaintenanceCycle_WhenClusterHealthy_AppliesIlmPolicies()
    {
        _indexManager.IsClusterHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);
        _indexManager.ApplyIlmPoliciesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var job = CreateJob();
        await job.RunMaintenanceCycleAsync(CancellationToken.None);

        await _indexManager.Received(1).ApplyIlmPoliciesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunMaintenanceCycle_WhenApplyIlmThrows_PropagatesException()
    {
        _indexManager.IsClusterHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);
        _indexManager.ApplyIlmPoliciesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("ES connection refused"));

        var job = CreateJob();

        await Assert.ThrowsAsync<HttpRequestException>(
            () => job.RunMaintenanceCycleAsync(CancellationToken.None));
    }

    [Fact]
    public async Task RunMaintenanceCycle_WhenHealthCheckThrows_PropagatesException()
    {
        _indexManager.IsClusterHealthyAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("unexpected error"));

        var job = CreateJob();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => job.RunMaintenanceCycleAsync(CancellationToken.None));
    }

    [Fact]
    public async Task RunMaintenanceCycle_WhenCancelled_DoesNotCallApply()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _indexManager.IsClusterHealthyAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var job = CreateJob();

        // OperationCanceledException may be thrown or the call may be skipped —
        // either way ApplyIlmPoliciesAsync must not be called
        try
        {
            await job.RunMaintenanceCycleAsync(cts.Token);
        }
        catch (OperationCanceledException) { }

        await _indexManager.DidNotReceive().ApplyIlmPoliciesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunMaintenanceCycle_WhenClusterHealthy_ChecksHealthBeforeApplying()
    {
        var callOrder = new List<string>();

        _indexManager.IsClusterHealthyAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callOrder.Add("health");
                return Task.FromResult(true);
            });

        _indexManager.ApplyIlmPoliciesAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callOrder.Add("apply");
                return Task.CompletedTask;
            });

        var job = CreateJob();
        await job.RunMaintenanceCycleAsync(CancellationToken.None);

        callOrder.Should().ContainInOrder("health", "apply");
    }
}
