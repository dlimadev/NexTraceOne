using FluentAssertions;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Configuration;

namespace NexTraceOne.BuildingBlocks.Observability.Tests.Telemetry;

/// <summary>
/// Testes da configuração de telemetria: TelemetryStoreOptions, RetentionPolicyOptions,
/// ProductStoreOptions, ObservabilityProviderOptions, CollectionModeOptions e CollectorOptions.
///
/// Valida: defaults seguros, separação Product Store vs provider de observabilidade,
/// política de retenção hot/warm/cold, limites do Collector,
/// provider configurável (Elastic/ClickHouse), modo de coleta (Collector/CLR Profiler).
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
    public void DefaultObservabilityProvider_ShouldUseElastic()
    {
        var options = new TelemetryStoreOptions();
        options.ObservabilityProvider.Provider.Should().Be("Elastic");
    }

    [Fact]
    public void DefaultObservabilityProvider_ClickHouse_ShouldBeDisabled()
    {
        var options = new TelemetryStoreOptions();
        options.ObservabilityProvider.ClickHouse.Enabled.Should().BeFalse();
    }

    [Fact]
    public void DefaultObservabilityProvider_Elastic_ShouldBeEnabled()
    {
        var options = new TelemetryStoreOptions();
        options.ObservabilityProvider.Elastic.Enabled.Should().BeTrue();
    }

    [Fact]
    public void DefaultCollectionMode_ShouldUseOpenTelemetryCollector()
    {
        var options = new TelemetryStoreOptions();
        options.CollectionMode.ActiveMode.Should().Be("OpenTelemetryCollector");
    }

    [Fact]
    public void DefaultCollectionMode_OpenTelemetryCollector_ShouldBeEnabled()
    {
        var options = new TelemetryStoreOptions();
        options.CollectionMode.OpenTelemetryCollector.Enabled.Should().BeTrue();
    }

    [Fact]
    public void DefaultCollectionMode_ClrProfiler_ShouldBeDisabled()
    {
        var options = new TelemetryStoreOptions();
        options.CollectionMode.ClrProfiler.Enabled.Should().BeFalse();
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

    [Fact]
    public void ClickHouseProvider_ShouldHaveConnectionString()
    {
        var options = new TelemetryStoreOptions();
        options.ObservabilityProvider.ClickHouse.ConnectionString.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ClickHouseProvider_ShouldHaveDedicatedDatabase()
    {
        var options = new TelemetryStoreOptions();
        options.ObservabilityProvider.ClickHouse.Database.Should().Be("nextraceone_obs");
    }

    [Fact]
    public void ClickHouseProvider_ShouldHaveRetentionDefaults()
    {
        var options = new TelemetryStoreOptions();
        options.ObservabilityProvider.ClickHouse.LogsRetentionDays.Should().BeGreaterThan(0);
        options.ObservabilityProvider.ClickHouse.TracesRetentionDays.Should().BeGreaterThan(0);
        options.ObservabilityProvider.ClickHouse.MetricsRetentionDays.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ElasticProvider_ShouldHaveIndexPrefix()
    {
        var options = new TelemetryStoreOptions();
        options.ObservabilityProvider.Elastic.IndexPrefix.Should().Be("nextraceone");
    }

    [Fact]
    public void ClrProfiler_ShouldDefaultToIISMode()
    {
        var options = new TelemetryStoreOptions();
        options.CollectionMode.ClrProfiler.Mode.Should().Be("IIS");
    }

    [Fact]
    public void ClrProfiler_ShouldDefaultToAutoInstrumentation()
    {
        var options = new TelemetryStoreOptions();
        options.CollectionMode.ClrProfiler.ProfilerType.Should().Be("AutoInstrumentation");
    }

    [Fact]
    public void ClrProfiler_ShouldExportViaCollectorByDefault()
    {
        var options = new TelemetryStoreOptions();
        options.CollectionMode.ClrProfiler.ExportTarget.Should().Be("Collector");
    }
}
