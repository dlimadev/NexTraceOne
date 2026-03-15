using FluentAssertions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using Xunit;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.Domain;

/// <summary>
/// Testes unitários para os enums do subdomínio Incidents.
/// Verificam definições de valores, contagem e consistência.
/// </summary>
public sealed class IncidentEnumTests
{
    // ── IncidentType ──────────────────────────────────────────────────

    [Fact]
    public void IncidentType_ShouldHaveSevenValues()
    {
        Enum.GetValues<IncidentType>().Should().HaveCount(7);
    }

    [Theory]
    [InlineData(IncidentType.ServiceDegradation, 0)]
    [InlineData(IncidentType.AvailabilityIssue, 1)]
    [InlineData(IncidentType.DependencyFailure, 2)]
    [InlineData(IncidentType.ContractImpact, 3)]
    [InlineData(IncidentType.MessagingIssue, 4)]
    [InlineData(IncidentType.BackgroundProcessingIssue, 5)]
    [InlineData(IncidentType.OperationalRegression, 6)]
    public void IncidentType_ShouldHaveExpectedValues(IncidentType type, int expected)
    {
        ((int)type).Should().Be(expected);
    }

    // ── IncidentSeverity ──────────────────────────────────────────────

    [Fact]
    public void IncidentSeverity_ShouldHaveFourValues()
    {
        Enum.GetValues<IncidentSeverity>().Should().HaveCount(4);
    }

    [Theory]
    [InlineData(IncidentSeverity.Warning, 0)]
    [InlineData(IncidentSeverity.Minor, 1)]
    [InlineData(IncidentSeverity.Major, 2)]
    [InlineData(IncidentSeverity.Critical, 3)]
    public void IncidentSeverity_ShouldHaveExpectedValues(IncidentSeverity severity, int expected)
    {
        ((int)severity).Should().Be(expected);
    }

    // ── IncidentStatus ────────────────────────────────────────────────

    [Fact]
    public void IncidentStatus_ShouldHaveSixValues()
    {
        Enum.GetValues<IncidentStatus>().Should().HaveCount(6);
    }

    [Theory]
    [InlineData(IncidentStatus.Open, 0)]
    [InlineData(IncidentStatus.Investigating, 1)]
    [InlineData(IncidentStatus.Mitigating, 2)]
    [InlineData(IncidentStatus.Monitoring, 3)]
    [InlineData(IncidentStatus.Resolved, 4)]
    [InlineData(IncidentStatus.Closed, 5)]
    public void IncidentStatus_ShouldHaveExpectedValues(IncidentStatus status, int expected)
    {
        ((int)status).Should().Be(expected);
    }

    // ── CorrelationConfidence ─────────────────────────────────────────

    [Fact]
    public void CorrelationConfidence_ShouldHaveFiveValues()
    {
        Enum.GetValues<CorrelationConfidence>().Should().HaveCount(5);
    }

    [Theory]
    [InlineData(CorrelationConfidence.NotAssessed, 0)]
    [InlineData(CorrelationConfidence.Low, 1)]
    [InlineData(CorrelationConfidence.Medium, 2)]
    [InlineData(CorrelationConfidence.High, 3)]
    [InlineData(CorrelationConfidence.Confirmed, 4)]
    public void CorrelationConfidence_ShouldHaveExpectedValues(CorrelationConfidence confidence, int expected)
    {
        ((int)confidence).Should().Be(expected);
    }

    // ── MitigationStatus ──────────────────────────────────────────────

    [Fact]
    public void MitigationStatus_ShouldHaveFiveValues()
    {
        Enum.GetValues<MitigationStatus>().Should().HaveCount(5);
    }

    [Theory]
    [InlineData(MitigationStatus.NotStarted, 0)]
    [InlineData(MitigationStatus.InProgress, 1)]
    [InlineData(MitigationStatus.Applied, 2)]
    [InlineData(MitigationStatus.Verified, 3)]
    [InlineData(MitigationStatus.Failed, 4)]
    public void MitigationStatus_ShouldHaveExpectedValues(MitigationStatus mitigationStatus, int expected)
    {
        ((int)mitigationStatus).Should().Be(expected);
    }
}
