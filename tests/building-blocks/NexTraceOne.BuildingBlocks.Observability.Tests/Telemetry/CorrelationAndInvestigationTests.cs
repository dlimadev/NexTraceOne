using FluentAssertions;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

namespace NexTraceOne.BuildingBlocks.Observability.Tests.Telemetry;

/// <summary>
/// Testes de correlação release/runtime e contexto investigativo.
/// Valida: correlation com markers, impacto de deploy, investigation context bundles,
/// referências para dados crus no Telemetry Store.
/// </summary>
public sealed class CorrelationAndInvestigationTests
{
    [Fact]
    public void ReleaseCorrelation_ShouldCapturePreAndPostDeployMetrics()
    {
        var correlation = new ReleaseRuntimeCorrelation
        {
            ReleaseId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            ServiceName = "payment-api",
            Environment = "production",
            DeployedAt = DateTimeOffset.UtcNow.AddMinutes(-30),
            MarkerType = "deployment",
            PreDeployErrorRate = 0.1,
            PreDeployLatencyP95Ms = 50.0,
            PreDeployRequestsPerMinute = 1200,
            PostDeployErrorRate = 2.5,
            PostDeployLatencyP95Ms = 350.0,
            PostDeployRequestsPerMinute = 1100,
            ImpactScore = 0.75,
            ImpactClassification = "degradation"
        };

        correlation.PreDeployErrorRate.Should().BeLessThan(correlation.PostDeployErrorRate,
            "neste cenário o deploy causou degradação");
        correlation.ImpactScore.Should().BeGreaterThan(0.5);
        correlation.ImpactClassification.Should().Be("degradation");
    }

    [Fact]
    public void ReleaseCorrelation_ShouldSupportDifferentMarkerTypes()
    {
        var markerTypes = new[] { "deployment", "promotion", "rollback", "canary", "feature_flag" };

        foreach (var marker in markerTypes)
        {
            var correlation = new ReleaseRuntimeCorrelation
            {
                ReleaseId = Guid.NewGuid(),
                ServiceId = Guid.NewGuid(),
                ServiceName = "api",
                Environment = "production",
                DeployedAt = DateTimeOffset.UtcNow,
                MarkerType = marker,
                ImpactClassification = "none"
            };

            correlation.MarkerType.Should().Be(marker);
        }
    }

    [Fact]
    public void ReleaseCorrelation_ShouldReferenceTelemetryStore()
    {
        var refIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        var correlation = new ReleaseRuntimeCorrelation
        {
            ReleaseId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            ServiceName = "order-api",
            Environment = "production",
            DeployedAt = DateTimeOffset.UtcNow,
            MarkerType = "deployment",
            ImpactClassification = "neutral",
            TelemetryReferenceIds = refIds
        };

        correlation.TelemetryReferenceIds.Should().HaveCount(3,
            "referências para traces/logs crus no Telemetry Store devem ser preservadas");
    }

    [Fact]
    public void InvestigationContext_ShouldBundleAllRelevantData()
    {
        var context = new InvestigationContext
        {
            Title = "Latency spike in payment-api after v2.3.0 deploy",
            InvestigationType = "deployment_impact",
            PrimaryServiceId = Guid.NewGuid(),
            PrimaryServiceName = "payment-api",
            Environment = "production",
            TimeWindowStart = DateTimeOffset.UtcNow.AddHours(-2),
            TimeWindowEnd = DateTimeOffset.UtcNow,
            Status = "open",
            AnomalySnapshotIds = [Guid.NewGuid(), Guid.NewGuid()],
            ReleaseCorrelationIds = [Guid.NewGuid()],
            TelemetryReferenceIds = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()],
            AffectedServiceIds = [Guid.NewGuid(), Guid.NewGuid()]
        };

        context.AnomalySnapshotIds.Should().HaveCount(2);
        context.ReleaseCorrelationIds.Should().HaveCount(1);
        context.TelemetryReferenceIds.Should().HaveCount(3);
        context.AffectedServiceIds.Should().HaveCount(2);
        context.Status.Should().Be("open");
    }

    [Fact]
    public void InvestigationContext_ShouldSupportAiSummary()
    {
        var context = new InvestigationContext
        {
            Title = "Error rate spike",
            InvestigationType = "anomaly",
            PrimaryServiceId = Guid.NewGuid(),
            PrimaryServiceName = "auth-api",
            Environment = "production",
            TimeWindowStart = DateTimeOffset.UtcNow.AddHours(-1),
            TimeWindowEnd = DateTimeOffset.UtcNow,
            Status = "in_progress",
            AiSummaryJson = """{"key_metrics":{"error_rate":5.2},"anomalies":2,"affected_services":3}"""
        };

        context.AiSummaryJson.Should().NotBeNullOrEmpty(
            "o sumário para IA permite análise automatizada pelo módulo AI Orchestration");
        context.AiSummaryJson.Should().Contain("error_rate");
    }

    [Fact]
    public void TelemetryReference_ShouldPointToExternalStore()
    {
        var reference = new TelemetryReference
        {
            SignalType = TelemetrySignalType.Traces,
            ExternalId = "abc123def456",
            BackendType = "tempo",
            AccessUri = "http://tempo:3200/api/traces/abc123def456",
            ServiceId = Guid.NewGuid(),
            ServiceName = "payment-api",
            Environment = "production",
            OriginalTimestamp = DateTimeOffset.UtcNow.AddMinutes(-5),
            CorrelationId = Guid.NewGuid()
        };

        reference.SignalType.Should().Be(TelemetrySignalType.Traces);
        reference.BackendType.Should().Be("tempo");
        reference.AccessUri.Should().Contain("traces");
        reference.CorrelationId.Should().NotBeNull(
            "referências devem ser navegáveis a partir de anomalias e correlações");
    }

    [Fact]
    public void TelemetryReference_ShouldSupportLogs()
    {
        var reference = new TelemetryReference
        {
            SignalType = TelemetrySignalType.Logs,
            ExternalId = "{job=\"nextraceone\"} |= \"ERROR\"",
            BackendType = "loki",
            OriginalTimestamp = DateTimeOffset.UtcNow
        };

        reference.SignalType.Should().Be(TelemetrySignalType.Logs);
        reference.BackendType.Should().Be("loki");
    }

    [Fact]
    public void AnomalySnapshot_ShouldCaptureSeverityAndDeviation()
    {
        var anomaly = new AnomalySnapshot
        {
            ServiceId = Guid.NewGuid(),
            ServiceName = "payment-api",
            Environment = "production",
            AnomalyType = "latency_spike",
            Severity = 3,
            Description = "P95 latency increased from 50ms to 350ms after deploy",
            MessageKey = "telemetry.anomaly.latency_spike",
            ObservedValue = 350.0,
            ExpectedValue = 50.0,
            DeviationPercent = 600.0,
            DetectedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            CorrelatedReleaseId = Guid.NewGuid()
        };

        anomaly.Severity.Should().Be(3);
        anomaly.DeviationPercent.Should().BeGreaterThan(100,
            "anomalia severa deve ter desvio significativo do baseline");
        anomaly.CorrelatedReleaseId.Should().NotBeNull(
            "quando possível, anomalia deve ser correlacionada com uma release");
        anomaly.ResolvedAt.Should().BeNull("anomalia ainda ativa");
    }

    [Fact]
    public void AnomalySnapshot_Resolved_ShouldHaveResolvedTimestamp()
    {
        var resolvedAt = DateTimeOffset.UtcNow;
        var anomaly = new AnomalySnapshot
        {
            ServiceId = Guid.NewGuid(),
            ServiceName = "auth-api",
            Environment = "production",
            AnomalyType = "error_rate_surge",
            Severity = 2,
            Description = "Error rate returned to baseline",
            MessageKey = "telemetry.anomaly.error_rate_surge",
            DetectedAt = DateTimeOffset.UtcNow.AddHours(-2),
            ResolvedAt = resolvedAt
        };

        anomaly.ResolvedAt.Should().Be(resolvedAt);
    }
}
