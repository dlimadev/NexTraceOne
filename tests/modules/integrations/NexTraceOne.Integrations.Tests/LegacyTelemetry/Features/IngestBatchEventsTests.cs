using System.Linq;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Features.IngestBatchEvents;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Requests;

namespace NexTraceOne.Integrations.Tests.LegacyTelemetry.Features;

public sealed class IngestBatchEventsValidatorTests
{
    private readonly IngestBatchEvents.Validator _validator = new();

    [Fact]
    public void Validate_EmptyEvents_Fails()
    {
        var command = new IngestBatchEvents.Command(new());
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_TooManyEvents_Fails()
    {
        var events = Enumerable.Range(0, 1001)
            .Select(i => new BatchEventRequest(
                null, null, $"JOB{i}", null, null, null, null, null, null, null, null, null, null, null, null))
            .ToList();

        var command = new IngestBatchEvents.Command(events);
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ValidSingleEvent_Passes()
    {
        var events = new List<BatchEventRequest>
        {
            new(null, null, "JOB1", null, null, null, null, null, null, null, null, null, null, null, null)
        };

        var command = new IngestBatchEvents.Command(events);
        var result = _validator.Validate(command);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_MissingJobName_Fails()
    {
        var events = new List<BatchEventRequest>
        {
            new(null, null, "", null, null, null, null, null, null, null, null, null, null, null, null)
        };

        var command = new IngestBatchEvents.Command(events);
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
    }
}
