using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexTraceOne.BackgroundWorkers.Jobs;
using NSubstitute;

namespace NexTraceOne.BackgroundWorkers.Tests.Jobs;

public sealed class IncidentProbabilityRefreshJobTests
{
    private readonly IServiceScopeFactory _serviceScopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly WorkerJobHealthRegistry _jobHealthRegistry = new();
    private readonly ILogger<IncidentProbabilityRefreshJob> _logger = Substitute.For<ILogger<IncidentProbabilityRefreshJob>>();

    [Fact]
    public void HealthCheckName_IsCorrect()
    {
        // Assert
        Assert.Equal("incident-probability-refresh-job", IncidentProbabilityRefreshJob.HealthCheckName);
    }

    [Fact]
    public void Job_CanBeInstantiated()
    {
        // Assert - Verify the job can be instantiated with correct configuration
        var job = new IncidentProbabilityRefreshJob(_serviceScopeFactory, _jobHealthRegistry, _logger);
        Assert.NotNull(job);
    }
}
