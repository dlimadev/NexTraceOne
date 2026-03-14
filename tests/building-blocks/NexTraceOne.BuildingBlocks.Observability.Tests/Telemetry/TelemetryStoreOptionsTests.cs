using FluentAssertions;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Configuration;

namespace NexTraceOne.BuildingBlocks.Observability.Tests.Telemetry;

/// <summary>
/// Testes da configuração de telemetria: TelemetryStoreOptions, RetentionPolicyOptions,
/// ProductStoreOptions, TelemetryBackendOptions e CollectorOptions.
///
/// Valida: defaults seguros, separação Product Store vs Telemetry Store,
/// política de retenção hot/warm/cold, limites do Collector.
/// </summary>
public sealed class TelemetryStoreOptionsTests
{
    [Fact]
    public void DefaultOptions_ShouldHaveCorrectSectionName()
    {
        TelemetryStoreOptions.SectionName.Should().Be("Telemetry");
    }

    [Fact]
    public void DefaultProductStore_ShouldUseMainConnectionString()
    {
        var options = new TelemetryStoreOptions();
        options.ProductStore.ConnectionStringName.Should().Be("NexTraceOne");
    }

    [Fact]
    public void DefaultProductStore_ShouldUseDedicatedSchema()
    {
        var options = new TelemetryStoreOptions();
        options.ProductStore.Schema.Should().Be("telemetry");
    }

    [Fact]
    public void DefaultProductStore_ShouldEnableTimePartitioning()
    {
        var options = new TelemetryStoreOptions();
        options.ProductStore.EnableTimePartitioning.Should().BeTrue();
    }

    [Fact]
    public void DefaultTelemetryStore_ShouldUseTempoForTraces()
    {
        var options = new TelemetryStoreOptions();
        options.TelemetryStore.TracesBackend.Should().Be("tempo");
    }

    [Fact]
    public void DefaultTelemetryStore_ShouldUseLokiForLogs()
    {
        var options = new TelemetryStoreOptions();
        options.TelemetryStore.LogsBackend.Should().Be("loki");
    }

    [Fact]
    public void DefaultTelemetryStore_ShouldNotHaveMetricsBackend()
    {
        var options = new TelemetryStoreOptions();
        options.TelemetryStore.MetricsBackend.Should().BeNull();
    }

    [Fact]
    public void DefaultCollector_ShouldUseStandardOtlpPorts()
    {
        var options = new TelemetryStoreOptions();
        options.Collector.OtlpGrpcEndpoint.Should().Contain("4317");
        options.Collector.OtlpHttpEndpoint.Should().Contain("4318");
    }

    [Fact]
    public void DefaultCollector_ShouldHaveMemoryProtection()
    {
        var options = new TelemetryStoreOptions();
        options.Collector.MemoryLimitMb.Should().BeGreaterThan(0);
        options.Collector.MemorySpikeLimitMb.Should().BeGreaterThan(0);
        options.Collector.MemorySpikeLimitMb.Should().BeLessThan(options.Collector.MemoryLimitMb);
    }

    [Fact]
    public void DefaultCollector_ShouldHaveBatchingConfig()
    {
        var options = new TelemetryStoreOptions();
        options.Collector.BatchSize.Should().BeGreaterThan(0);
        options.Collector.BatchTimeoutMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public void DefaultCollector_ShouldHaveFullSamplingForDev()
    {
        var options = new TelemetryStoreOptions();
        options.Collector.TracesSamplingRate.Should().Be(1.0);
    }
}
