using NexTraceOne.Integrations.Application.LegacyTelemetry.Parsers;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Requests;
using NexTraceOne.Integrations.Domain.LegacyTelemetry;

namespace NexTraceOne.Integrations.Tests.LegacyTelemetry.Parsers;

public sealed class BatchEventParserTests
{
    private readonly BatchEventParser _parser = new();

    [Fact]
    public void Parse_CompletedJob_ReturnsInfoSeverity()
    {
        var request = new BatchEventRequest(
            Provider: "JES2", CorrelationId: null, JobName: "PAYROLL1",
            JobId: "JOB00123", StepName: "STEP01", ProgramName: "PGMPAY01",
            ReturnCode: "0000", Status: "completed", SystemName: "SYS1",
            LparName: "LPAR01", StartedAt: DateTimeOffset.UtcNow.AddMinutes(-5),
            CompletedAt: DateTimeOffset.UtcNow, DurationMs: 300000,
            ChainName: "DAILY_PAY", Metadata: null);

        var result = _parser.Parse(request);

        Assert.Equal("batch_execution", result.EventType);
        Assert.Equal(LegacyEventSourceType.Batch, result.SourceType);
        Assert.Equal(LegacySeverity.Info, result.Severity);
        Assert.Equal("SYS1", result.SystemName);
        Assert.Equal("LPAR01", result.LparName);
        Assert.Equal("PAYROLL1", result.ServiceName);
        Assert.NotEmpty(result.EventId);
        Assert.Contains("job_id", result.Attributes.Keys);
        Assert.Equal("JOB00123", result.Attributes["job_id"]);
    }

    [Fact]
    public void Parse_AbendedJob_ReturnsCriticalSeverity()
    {
        var request = new BatchEventRequest(
            Provider: "CA7", CorrelationId: null, JobName: "BATCH99",
            JobId: "JOB99999", StepName: "STEP03", ProgramName: "PGMFAIL",
            ReturnCode: "ABEND S0C7", Status: "abended", SystemName: "SYS2",
            LparName: "LPAR02", StartedAt: DateTimeOffset.UtcNow.AddMinutes(-1),
            CompletedAt: DateTimeOffset.UtcNow, DurationMs: 60000,
            ChainName: null, Metadata: null);

        var result = _parser.Parse(request);

        Assert.Equal(LegacySeverity.Critical, result.Severity);
    }

    [Fact]
    public void Parse_FailedJob_ReturnsErrorSeverity()
    {
        var request = new BatchEventRequest(
            Provider: "JES2", CorrelationId: null, JobName: "FAILJOB",
            JobId: "JOB00456", StepName: null, ProgramName: null,
            ReturnCode: "0008", Status: "failed", SystemName: null,
            LparName: null, StartedAt: null, CompletedAt: null, DurationMs: null,
            ChainName: null, Metadata: null);

        var result = _parser.Parse(request);

        Assert.Equal(LegacySeverity.Error, result.Severity);
    }

    [Fact]
    public void Parse_WarningReturnCode_ReturnsWarning()
    {
        var request = new BatchEventRequest(
            Provider: "JES2", CorrelationId: "corr-123", JobName: "WARNJ",
            JobId: null, StepName: null, ProgramName: null,
            ReturnCode: "0012", Status: "completed", SystemName: null,
            LparName: null, StartedAt: null, CompletedAt: null, DurationMs: null,
            ChainName: null, Metadata: new Dictionary<string, string> { { "custom_key", "custom_val" } });

        var result = _parser.Parse(request);

        Assert.Equal(LegacySeverity.Warning, result.Severity);
        Assert.Equal("custom_val", result.Attributes["custom_key"]);
        Assert.Equal("corr-123", result.Attributes["correlation_id"]);
    }

    [Fact]
    public void Parse_MinimalRequest_SetsDefaults()
    {
        var request = new BatchEventRequest(
            Provider: null, CorrelationId: null, JobName: "MINJ",
            JobId: null, StepName: null, ProgramName: null,
            ReturnCode: null, Status: null, SystemName: null,
            LparName: null, StartedAt: null, CompletedAt: null, DurationMs: null,
            ChainName: null, Metadata: null);

        var result = _parser.Parse(request);

        Assert.Equal(LegacySeverity.Info, result.Severity);
        Assert.Equal("MINJ", result.ServiceName);
        Assert.NotEmpty(result.EventId);
        Assert.True(result.Timestamp <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Parse_WithAllMetadata_IncludesAllAttributes()
    {
        var metadata = new Dictionary<string, string>
        {
            { "region", "US-EAST" },
            { "scheduler", "TWS" }
        };

        var request = new BatchEventRequest(
            Provider: "TWS", CorrelationId: "corr-456", JobName: "JOB_META",
            JobId: "J001", StepName: "S01", ProgramName: "PGM01",
            ReturnCode: "0000", Status: "completed", SystemName: "SYS3",
            LparName: "LP3", StartedAt: DateTimeOffset.UtcNow.AddHours(-1),
            CompletedAt: DateTimeOffset.UtcNow, DurationMs: 3600000,
            ChainName: "CHAIN1", Metadata: metadata);

        var result = _parser.Parse(request);

        Assert.Contains("provider", result.Attributes.Keys);
        Assert.Contains("job_id", result.Attributes.Keys);
        Assert.Contains("step_name", result.Attributes.Keys);
        Assert.Contains("program_name", result.Attributes.Keys);
        Assert.Contains("return_code", result.Attributes.Keys);
        Assert.Contains("chain_name", result.Attributes.Keys);
        Assert.Contains("duration_ms", result.Attributes.Keys);
        Assert.Contains("region", result.Attributes.Keys);
        Assert.Contains("scheduler", result.Attributes.Keys);
    }
}
