using FluentAssertions;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Models;

namespace NexTraceOne.BuildingBlocks.Observability.Tests.Alerting;

/// <summary>
/// Testes do AlertPayload: criação, timestamp padrão e severidades.
/// </summary>
public sealed class AlertPayloadTests
{
    [Fact]
    public void AlertPayload_Creation_SetsRequiredFields()
    {
        var payload = new AlertPayload
        {
            Title = "High CPU",
            Description = "CPU usage above 90%",
            Severity = AlertSeverity.Critical,
            Source = "monitoring-service"
        };

        payload.Title.Should().Be("High CPU");
        payload.Description.Should().Be("CPU usage above 90%");
        payload.Severity.Should().Be(AlertSeverity.Critical);
        payload.Source.Should().Be("monitoring-service");
    }

    [Fact]
    public void AlertPayload_DefaultTimestamp_IsSet()
    {
        var before = DateTimeOffset.UtcNow;

        var payload = new AlertPayload
        {
            Title = "Test",
            Description = "Test description",
            Severity = AlertSeverity.Info,
            Source = "test"
        };

        var after = DateTimeOffset.UtcNow;

        payload.Timestamp.Should().BeOnOrAfter(before);
        payload.Timestamp.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void AlertPayload_DefaultContext_IsEmptyDictionary()
    {
        var payload = new AlertPayload
        {
            Title = "Test",
            Description = "Test",
            Severity = AlertSeverity.Info,
            Source = "test"
        };

        payload.Context.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void AlertPayload_CorrelationId_IsNullByDefault()
    {
        var payload = new AlertPayload
        {
            Title = "Test",
            Description = "Test",
            Severity = AlertSeverity.Warning,
            Source = "test"
        };

        payload.CorrelationId.Should().BeNull();
    }

    [Fact]
    public void AlertPayload_WithContext_StoresKeyValuePairs()
    {
        var payload = new AlertPayload
        {
            Title = "Test",
            Description = "Test",
            Severity = AlertSeverity.Error,
            Source = "test",
            Context = new Dictionary<string, string>
            {
                ["service"] = "orders-api",
                ["region"] = "eu-west-1"
            }
        };

        payload.Context.Should().HaveCount(2);
        payload.Context["service"].Should().Be("orders-api");
        payload.Context["region"].Should().Be("eu-west-1");
    }

    [Theory]
    [InlineData(AlertSeverity.Info)]
    [InlineData(AlertSeverity.Warning)]
    [InlineData(AlertSeverity.Error)]
    [InlineData(AlertSeverity.Critical)]
    public void AlertPayload_SeverityValues_AreValid(AlertSeverity severity)
    {
        var payload = new AlertPayload
        {
            Title = "Test",
            Description = "Test",
            Severity = severity,
            Source = "test"
        };

        payload.Severity.Should().Be(severity);
        Enum.IsDefined(payload.Severity).Should().BeTrue();
    }
}
