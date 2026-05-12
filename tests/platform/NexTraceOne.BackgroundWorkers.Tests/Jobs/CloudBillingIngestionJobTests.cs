using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexTraceOne.BackgroundWorkers.Jobs;
using NSubstitute;

namespace NexTraceOne.BackgroundWorkers.Tests.Jobs;

public sealed class CloudBillingIngestionJobTests
{
    private readonly IServiceScopeFactory _serviceScopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly WorkerJobHealthRegistry _jobHealthRegistry = new();
    private readonly ILogger<CloudBillingIngestionJob> _logger = Substitute.For<ILogger<CloudBillingIngestionJob>>();

    [Fact]
    public void HealthCheckName_IsCorrect()
    {
        // Assert
        Assert.Equal("cloud-billing-ingestion-job", CloudBillingIngestionJob.HealthCheckName);
    }

    [Fact]
    public void Job_CanBeInstantiated()
    {
        // Assert - Verify the job can be instantiated with correct configuration
        var job = new CloudBillingIngestionJob(_serviceScopeFactory, _jobHealthRegistry, _logger);
        Assert.NotNull(job);
    }
}
