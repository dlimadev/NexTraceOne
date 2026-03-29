using NexTraceOne.Integrations.Application.LegacyTelemetry.Parsers;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Requests;
using NexTraceOne.Integrations.Domain.LegacyTelemetry;

namespace NexTraceOne.Integrations.Tests.LegacyTelemetry.Parsers;

public sealed class MqEventParserTests
{
    private readonly MqEventParser _parser = new();

    [Fact]
    public void Parse_NormalStatistics_ReturnsInfoSeverity()
    {
        var request = new MqEventRequest(
            Provider: "IBM MQ", CorrelationId: null, QueueManagerName: "QMGR01",
            QueueName: "APP.REQ.QUEUE", ChannelName: null,
            EventType: "statistics", QueueDepth: 10, MaxDepth: 1000,
            EnqueueCount: 5000, DequeueCount: 4990,
            ChannelStatus: null, EventTimestamp: DateTimeOffset.UtcNow, Metadata: null);

        var result = _parser.Parse(request);

        Assert.Equal("mq_statistics", result.EventType);
        Assert.Equal(LegacyEventSourceType.Mq, result.SourceType);
        Assert.Equal(LegacySeverity.Info, result.Severity);
        Assert.Equal("QMGR01", result.SystemName);
        Assert.Equal("APP.REQ.QUEUE", result.AssetName);
    }

    [Fact]
    public void Parse_DeadLetterMessage_ReturnsErrorSeverity()
    {
        var request = new MqEventRequest(
            Provider: "IBM MQ", CorrelationId: null, QueueManagerName: "QMGR01",
            QueueName: "DLQ", ChannelName: null,
            EventType: "dlq_message", QueueDepth: 5, MaxDepth: 100,
            EnqueueCount: null, DequeueCount: null,
            ChannelStatus: null, EventTimestamp: DateTimeOffset.UtcNow, Metadata: null);

        var result = _parser.Parse(request);

        Assert.Equal("mq_dead_letter", result.EventType);
        Assert.Equal(LegacySeverity.Error, result.Severity);
    }

    [Fact]
    public void Parse_HighQueueDepth90Pct_ReturnsCriticalSeverity()
    {
        var request = new MqEventRequest(
            Provider: "OMEGAMON", CorrelationId: null, QueueManagerName: "QM02",
            QueueName: "HIGH.QUEUE", ChannelName: null,
            EventType: "depth_threshold", QueueDepth: 950, MaxDepth: 1000,
            EnqueueCount: null, DequeueCount: null,
            ChannelStatus: null, EventTimestamp: DateTimeOffset.UtcNow, Metadata: null);

        var result = _parser.Parse(request);

        Assert.Equal(LegacySeverity.Critical, result.Severity);
    }

    [Fact]
    public void Parse_QueueDepth70Pct_ReturnsWarningSeverity()
    {
        var request = new MqEventRequest(
            Provider: "IBM MQ", CorrelationId: null, QueueManagerName: "QM03",
            QueueName: "MED.QUEUE", ChannelName: null,
            EventType: "statistics", QueueDepth: 750, MaxDepth: 1000,
            EnqueueCount: null, DequeueCount: null,
            ChannelStatus: null, EventTimestamp: DateTimeOffset.UtcNow, Metadata: null);

        var result = _parser.Parse(request);

        Assert.Equal(LegacySeverity.Warning, result.Severity);
    }

    [Fact]
    public void Parse_StoppedChannel_ReturnsWarningSeverity()
    {
        var request = new MqEventRequest(
            Provider: "IBM MQ", CorrelationId: null, QueueManagerName: "QM04",
            QueueName: null, ChannelName: "CHANNEL.A",
            EventType: "channel_status", QueueDepth: null, MaxDepth: null,
            EnqueueCount: null, DequeueCount: null,
            ChannelStatus: "stopped", EventTimestamp: DateTimeOffset.UtcNow, Metadata: null);

        var result = _parser.Parse(request);

        Assert.Equal("mq_channel_status", result.EventType);
        Assert.Equal(LegacySeverity.Warning, result.Severity);
        Assert.Equal("CHANNEL.A", result.AssetName);
    }

    [Fact]
    public void Parse_MinimalRequest_SetsDefaults()
    {
        var request = new MqEventRequest(
            Provider: null, CorrelationId: null, QueueManagerName: null,
            QueueName: null, ChannelName: null,
            EventType: null, QueueDepth: null, MaxDepth: null,
            EnqueueCount: null, DequeueCount: null,
            ChannelStatus: null, EventTimestamp: null, Metadata: null);

        var result = _parser.Parse(request);

        Assert.Equal("mq_operational", result.EventType);
        Assert.Equal(LegacySeverity.Info, result.Severity);
        Assert.NotEmpty(result.EventId);
    }

    [Fact]
    public void Parse_WithMetadata_IncludesCustomAttributes()
    {
        var metadata = new Dictionary<string, string> { { "region", "EU" }, { "cluster", "PROD1" } };
        var request = new MqEventRequest(
            Provider: "IBM MQ", CorrelationId: "corr-789", QueueManagerName: "QM_EU",
            QueueName: "EU.QUEUE", ChannelName: null,
            EventType: "statistics", QueueDepth: 10, MaxDepth: 500,
            EnqueueCount: 100, DequeueCount: 90,
            ChannelStatus: null, EventTimestamp: DateTimeOffset.UtcNow, Metadata: metadata);

        var result = _parser.Parse(request);

        Assert.Contains("region", result.Attributes.Keys);
        Assert.Contains("cluster", result.Attributes.Keys);
        Assert.Contains("enqueue_count", result.Attributes.Keys);
        Assert.Contains("dequeue_count", result.Attributes.Keys);
    }
}
