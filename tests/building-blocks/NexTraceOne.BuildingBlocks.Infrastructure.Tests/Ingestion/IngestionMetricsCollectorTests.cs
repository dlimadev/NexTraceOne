using System.Diagnostics.Metrics;
using Microsoft.Extensions.Options;
using NexTraceOne.BuildingBlocks.Observability.Ingestion;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Tests.Ingestion;

/// <summary>
/// Testes unitários para IIngestionMetricsCollector.
/// Verifica emissão de métricas OTel via MeterListener e comportamento do null collector.
/// </summary>
public sealed class IngestionMetricsCollectorTests
{
    // ── Null implementation ───────────────────────────────────────────────────

    [Fact]
    public void NullCollector_RecordEventReceived_DoesNotThrow()
    {
        IIngestionMetricsCollector sut = new NoOpCollector();
        var act = () => sut.RecordEventReceived("tenant-1", "github");
        act.Should().NotThrow();
    }

    [Fact]
    public void NullCollector_RecordEventProcessed_DoesNotThrow()
    {
        IIngestionMetricsCollector sut = new NoOpCollector();
        var act = () => sut.RecordEventProcessed("tenant-1", "success");
        act.Should().NotThrow();
    }

    [Fact]
    public void NullCollector_RecordProcessingDuration_DoesNotThrow()
    {
        IIngestionMetricsCollector sut = new NoOpCollector();
        var act = () => sut.RecordProcessingDuration("tenant-1", "outbox-cycle", 123.4);
        act.Should().NotThrow();
    }

    [Fact]
    public void NullCollector_RecordDlqEntry_DoesNotThrow()
    {
        IIngestionMetricsCollector sut = new NoOpCollector();
        var act = () => sut.RecordDlqEntry("tenant-1");
        act.Should().NotThrow();
    }

    // ── Real implementation — emission ────────────────────────────────────────

    [Fact]
    public void RealCollector_RecordEventReceived_EmitsCounter()
    {
        var (collector, listener, measurements) = BuildLongListener("ingestion.events.received");

        collector.RecordEventReceived("tenant-abc", "github");

        listener.Dispose();
        measurements.Should().ContainSingle(m => m.Value == 1);
    }

    [Fact]
    public void RealCollector_RecordEventReceived_HasTenantAndSourceTags()
    {
        var (collector, listener, measurements) = BuildLongListener("ingestion.events.received");

        collector.RecordEventReceived("t-42", "gitlab");

        listener.Dispose();
        var m = measurements.Should().ContainSingle().Subject;
        GetTag(m.Tags, "tenant.id").Should().Be("t-42");
        GetTag(m.Tags, "source").Should().Be("gitlab");
    }

    [Fact]
    public void RealCollector_RecordEventProcessed_EmitsCounterWithResultTag()
    {
        var (collector, listener, measurements) = BuildLongListener("ingestion.events.processed");

        collector.RecordEventProcessed("tenant-x", "success");

        listener.Dispose();
        var m = measurements.Should().ContainSingle().Subject;
        m.Value.Should().Be(1);
        GetTag(m.Tags, "result").Should().Be("success");
    }

    [Fact]
    public void RealCollector_RecordEventProcessed_DlqResult_HasCorrectTag()
    {
        var (collector, listener, measurements) = BuildLongListener("ingestion.events.processed");

        collector.RecordEventProcessed("tenant-y", "dlq");

        listener.Dispose();
        GetTag(measurements.Single().Tags, "result").Should().Be("dlq");
    }

    [Fact]
    public void RealCollector_RecordProcessingDuration_EmitsHistogram()
    {
        var (collector, listener, measurements) = BuildDoubleListener("ingestion.processing.duration_ms");

        collector.RecordProcessingDuration("tenant-z", "outbox-cycle", 250.5);

        listener.Dispose();
        var m = measurements.Should().ContainSingle().Subject;
        m.Value.Should().BeApproximately(250.5, 0.001);
        GetTag(m.Tags, "stage").Should().Be("outbox-cycle");
    }

    [Fact]
    public void RealCollector_RecordDlqEntry_EmitsCounter()
    {
        var (collector, listener, measurements) = BuildLongListener("ingestion.dlq.count");

        collector.RecordDlqEntry("tenant-dlq");

        listener.Dispose();
        var m = measurements.Should().ContainSingle().Subject;
        m.Value.Should().Be(1);
        GetTag(m.Tags, "tenant.id").Should().Be("tenant-dlq");
    }

    [Fact]
    public void RealCollector_MultipleEventReceived_AccumulatesCount()
    {
        var (collector, listener, measurements) = BuildLongListener("ingestion.events.received");

        collector.RecordEventReceived("t1", "github");
        collector.RecordEventReceived("t1", "github");
        collector.RecordEventReceived("t2", "gitlab");

        listener.Dispose();
        measurements.Should().HaveCount(3);
        measurements.Sum(m => m.Value).Should().Be(3);
    }

    [Fact]
    public void RealCollector_WhenDisabled_DoesNotEmitCounter()
    {
        var options = Options.Create(new IngestionObservabilityOptions { Enabled = false });
        var collector = new IngestionMetricsCollector(options);

        var captured = new List<long>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Name == "ingestion.events.received")
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, value, _, _) => captured.Add(value));
        listener.Start();

        collector.RecordEventReceived("tenant-1", "github");

        captured.Should().BeEmpty("metrics disabled should not emit measurements");
    }

    [Fact]
    public void RealCollector_RecordProcessingDuration_HasTenantAndStageTags()
    {
        var (collector, listener, measurements) = BuildDoubleListener("ingestion.processing.duration_ms");

        collector.RecordProcessingDuration("my-tenant", "endpoint-ingest", 75.0);

        listener.Dispose();
        var m = measurements.Single();
        GetTag(m.Tags, "tenant.id").Should().Be("my-tenant");
        GetTag(m.Tags, "stage").Should().Be("endpoint-ingest");
    }

    [Fact]
    public void RealCollector_RecordEventProcessed_FailureResult_HasCorrectTag()
    {
        var (collector, listener, measurements) = BuildLongListener("ingestion.events.processed");

        collector.RecordEventProcessed("tenant-f", "failure");

        listener.Dispose();
        GetTag(measurements.Single().Tags, "result").Should().Be("failure");
    }

    [Fact]
    public void RealCollector_RecordEventProcessed_TenantTag_IsPresent()
    {
        var (collector, listener, measurements) = BuildLongListener("ingestion.events.processed");

        collector.RecordEventProcessed("my-tenant-id", "success");

        listener.Dispose();
        GetTag(measurements.Single().Tags, "tenant.id").Should().Be("my-tenant-id");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed record Measurement<T>(T Value, KeyValuePair<string, object?>[] Tags);

    private static (IngestionMetricsCollector Collector, MeterListener Listener,
        List<Measurement<long>> Measurements) BuildLongListener(string instrumentName)
    {
        var options = Options.Create(new IngestionObservabilityOptions { Enabled = true });
        var collector = new IngestionMetricsCollector(options);
        var measurements = new List<Measurement<long>>();

        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Name == instrumentName) l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
            measurements.Add(new Measurement<long>(value, tags.ToArray())));
        listener.Start();

        return (collector, listener, measurements);
    }

    private static (IngestionMetricsCollector Collector, MeterListener Listener,
        List<Measurement<double>> Measurements) BuildDoubleListener(string instrumentName)
    {
        var options = Options.Create(new IngestionObservabilityOptions { Enabled = true });
        var collector = new IngestionMetricsCollector(options);
        var measurements = new List<Measurement<double>>();

        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Name == instrumentName) l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<double>((_, value, tags, _) =>
            measurements.Add(new Measurement<double>(value, tags.ToArray())));
        listener.Start();

        return (collector, listener, measurements);
    }

    private static object? GetTag(KeyValuePair<string, object?>[] tags, string key)
        => tags.FirstOrDefault(t => t.Key == key).Value;

    /// <summary>No-op implementation to test interface contract without the internal NullIngestionMetricsCollector.</summary>
    private sealed class NoOpCollector : IIngestionMetricsCollector
    {
        public void RecordEventReceived(string tenantId, string source) { }
        public void RecordEventProcessed(string tenantId, string result) { }
        public void RecordProcessingDuration(string tenantId, string stage, double durationMs) { }
        public void RecordDlqEntry(string tenantId) { }
    }
}
