using FluentAssertions;
using NexTraceOne.BuildingBlocks.Observability.Metrics;
using NexTraceOne.BuildingBlocks.Observability.Tracing;

namespace NexTraceOne.BuildingBlocks.Observability.Tests.Telemetry;

/// <summary>
/// Testes de OpenTelemetry readiness: activity sources, meters e instrumentação.
/// Valida: todos os sources estão registrados, meters cobrem métricas de telemetria,
/// nomes seguem convenção semântica OpenTelemetry.
/// </summary>
public sealed class OpenTelemetryReadinessTests
{
    [Fact]
    public void ActivitySources_ShouldCoverAllPlatformOperations()
    {
        NexTraceActivitySources.Commands.Should().NotBeNull();
        NexTraceActivitySources.Queries.Should().NotBeNull();
        NexTraceActivitySources.Events.Should().NotBeNull();
        NexTraceActivitySources.ExternalHttp.Should().NotBeNull();
        NexTraceActivitySources.TelemetryPipeline.Should().NotBeNull();
    }

    [Fact]
    public void ActivitySources_ShouldFollowNamingConvention()
    {
        NexTraceActivitySources.Commands.Name.Should().StartWith("NexTraceOne.");
        NexTraceActivitySources.Queries.Name.Should().StartWith("NexTraceOne.");
        NexTraceActivitySources.Events.Name.Should().StartWith("NexTraceOne.");
        NexTraceActivitySources.ExternalHttp.Name.Should().StartWith("NexTraceOne.");
        NexTraceActivitySources.TelemetryPipeline.Name.Should().StartWith("NexTraceOne.");
    }

    [Fact]
    public void TelemetryPipelineSource_ShouldExistForSelfMonitoring()
    {
        NexTraceActivitySources.TelemetryPipeline.Name
            .Should().Contain("TelemetryPipeline",
                "o pipeline de telemetria precisa de tracing próprio para observabilidade de si mesmo");
    }

    [Fact]
    public void Meters_ShouldHaveConsistentNaming()
    {
        NexTraceMeters.MeterName.Should().Be("NexTraceOne");
    }

    [Fact]
    public void Meters_ShouldCoverBusinessOperations()
    {
        NexTraceMeters.DeploymentsNotified.Should().NotBeNull();
        NexTraceMeters.WorkflowsInitiated.Should().NotBeNull();
        NexTraceMeters.BlastRadiusDuration.Should().NotBeNull();
    }

    [Fact]
    public void Meters_ShouldCoverTelemetryPipelineOperations()
    {
        NexTraceMeters.ServiceMetricsWritten.Should().NotBeNull();
        NexTraceMeters.DependencyMetricsWritten.Should().NotBeNull();
        NexTraceMeters.TelemetryReferencesCreated.Should().NotBeNull();
        NexTraceMeters.AnomaliesDetected.Should().NotBeNull();
        NexTraceMeters.ReleaseCorrelationsCreated.Should().NotBeNull();
        NexTraceMeters.AggregationDuration.Should().NotBeNull();
        NexTraceMeters.RetentionPurgedRecords.Should().NotBeNull();
        NexTraceMeters.TopologyEntriesUpdated.Should().NotBeNull();
        NexTraceMeters.InvestigationContextsCreated.Should().NotBeNull();
    }

    [Fact]
    public void Meters_TelemetryMetrics_ShouldFollowNamingConvention()
    {
        NexTraceMeters.ServiceMetricsWritten.Name
            .Should().StartWith("nextraceone.telemetry.");
        NexTraceMeters.AnomaliesDetected.Name
            .Should().StartWith("nextraceone.telemetry.");
        NexTraceMeters.RetentionPurgedRecords.Name
            .Should().StartWith("nextraceone.telemetry.");
    }
}
