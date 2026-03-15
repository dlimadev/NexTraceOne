using FluentAssertions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetDomainReliabilitySummary;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityCoverage;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityDetail;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityTrend;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetTeamReliabilitySummary;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetTeamReliabilityTrend;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.ListServiceReliability;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;
using Xunit;

namespace NexTraceOne.OperationalIntelligence.Tests.Reliability.Application;

/// <summary>
/// Testes unitários para as features do subdomínio Reliability.
/// Verificam handlers, validators e respostas de todas as queries.
/// </summary>
public sealed class ReliabilityFeatureTests
{
    // ── ListServiceReliability ───────────────────────────────────────

    [Fact]
    public async Task ListServiceReliability_WithNoFilters_ShouldReturnAllItems()
    {
        var handler = new ListServiceReliability.Handler();
        var query = new ListServiceReliability.Query(null, null, null, null, null, null, null, null, 1, 100);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().NotBeEmpty();
        result.Value.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ListServiceReliability_FilterByStatus_ShouldReturnFiltered()
    {
        var handler = new ListServiceReliability.Handler();
        var query = new ListServiceReliability.Query(null, null, null, null, ReliabilityStatus.Degraded, null, null, null, 1, 100);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().AllSatisfy(i => i.ReliabilityStatus.Should().Be(ReliabilityStatus.Degraded));
    }

    [Fact]
    public async Task ListServiceReliability_FilterBySearch_ShouldReturnMatching()
    {
        var handler = new ListServiceReliability.Handler();
        var query = new ListServiceReliability.Query(null, null, null, null, null, null, null, "payment", 1, 100);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
    }

    [Fact]
    public void ListServiceReliability_Validator_ShouldRejectInvalidPage()
    {
        var validator = new ListServiceReliability.Validator();
        var query = new ListServiceReliability.Query(null, null, null, null, null, null, null, null, 0, 10);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListServiceReliability_Validator_ShouldAcceptValidQuery()
    {
        var validator = new ListServiceReliability.Validator();
        var query = new ListServiceReliability.Query("team-a", null, null, null, null, null, null, null, 1, 20);

        var result = validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    // ── GetServiceReliabilityDetail ──────────────────────────────────

    [Fact]
    public async Task GetServiceReliabilityDetail_KnownService_ShouldReturnDetail()
    {
        var handler = new GetServiceReliabilityDetail.Handler();
        var query = new GetServiceReliabilityDetail.Query("svc-order-api");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Identity.ServiceId.Should().Be("svc-order-api");
        result.Value.Status.Should().Be(ReliabilityStatus.Healthy);
        result.Value.RecentChanges.Should().NotBeEmpty();
        result.Value.Coverage.HasOperationalSignals.Should().BeTrue();
    }

    [Fact]
    public async Task GetServiceReliabilityDetail_DegradedService_ShouldShowFlags()
    {
        var handler = new GetServiceReliabilityDetail.Handler();
        var query = new GetServiceReliabilityDetail.Query("svc-payment-gateway");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ReliabilityStatus.Degraded);
        result.Value.ActiveFlags.HasFlag(OperationalFlag.AnomalyDetected).Should().BeTrue();
        result.Value.ActiveFlags.HasFlag(OperationalFlag.RecentChangeImpact).Should().BeTrue();
    }

    [Fact]
    public async Task GetServiceReliabilityDetail_UnknownService_ShouldReturnNotFound()
    {
        var handler = new GetServiceReliabilityDetail.Handler();
        var query = new GetServiceReliabilityDetail.Query("svc-nonexistent");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void GetServiceReliabilityDetail_Validator_ShouldRejectEmptyId()
    {
        var validator = new GetServiceReliabilityDetail.Validator();
        var query = new GetServiceReliabilityDetail.Query("");

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    // ── GetTeamReliabilitySummary ────────────────────────────────────

    [Fact]
    public async Task GetTeamReliabilitySummary_KnownTeam_ShouldReturnSummary()
    {
        var handler = new GetTeamReliabilitySummary.Handler();
        var query = new GetTeamReliabilitySummary.Query("order-squad");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TeamId.Should().Be("order-squad");
        result.Value.TotalServices.Should().BeGreaterThan(0);
        result.Value.HealthyServices.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetTeamReliabilitySummary_UnknownTeam_ShouldReturnZeros()
    {
        var handler = new GetTeamReliabilitySummary.Handler();
        var query = new GetTeamReliabilitySummary.Query("unknown-squad");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServices.Should().Be(0);
    }

    // ── GetDomainReliabilitySummary ──────────────────────────────────

    [Fact]
    public async Task GetDomainReliabilitySummary_KnownDomain_ShouldReturnSummary()
    {
        var handler = new GetDomainReliabilitySummary.Handler();
        var query = new GetDomainReliabilitySummary.Query("orders");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DomainId.Should().Be("Orders");
        result.Value.TotalServices.Should().BeGreaterThan(0);
    }

    // ── GetServiceReliabilityTrend ──────────────────────────────────

    [Fact]
    public async Task GetServiceReliabilityTrend_KnownService_ShouldReturnTrend()
    {
        var handler = new GetServiceReliabilityTrend.Handler();
        var query = new GetServiceReliabilityTrend.Query("svc-order-api");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Direction.Should().Be(TrendDirection.Stable);
        result.Value.DataPoints.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetServiceReliabilityTrend_DecliningService_ShouldShowDecline()
    {
        var handler = new GetServiceReliabilityTrend.Handler();
        var query = new GetServiceReliabilityTrend.Query("svc-payment-gateway");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Direction.Should().Be(TrendDirection.Declining);
    }

    // ── GetTeamReliabilityTrend ─────────────────────────────────────

    [Fact]
    public async Task GetTeamReliabilityTrend_ShouldReturnTrend()
    {
        var handler = new GetTeamReliabilityTrend.Handler();
        var query = new GetTeamReliabilityTrend.Query("order-squad");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TeamId.Should().Be("order-squad");
        result.Value.DataPoints.Should().NotBeEmpty();
    }

    // ── GetServiceReliabilityCoverage ────────────────────────────────

    [Fact]
    public async Task GetServiceReliabilityCoverage_WellCoveredService_ShouldShowFullCoverage()
    {
        var handler = new GetServiceReliabilityCoverage.Handler();
        var query = new GetServiceReliabilityCoverage.Query("svc-order-api");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasOperationalSignals.Should().BeTrue();
        result.Value.HasRunbook.Should().BeTrue();
        result.Value.HasOwner.Should().BeTrue();
    }

    [Fact]
    public async Task GetServiceReliabilityCoverage_PoorlyCoveredService_ShouldShowGaps()
    {
        var handler = new GetServiceReliabilityCoverage.Handler();
        var query = new GetServiceReliabilityCoverage.Query("svc-report-scheduler");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasOperationalSignals.Should().BeFalse();
        result.Value.HasRunbook.Should().BeFalse();
        result.Value.HasOwner.Should().BeTrue();
    }

    [Fact]
    public void GetServiceReliabilityCoverage_Validator_ShouldRejectEmptyId()
    {
        var validator = new GetServiceReliabilityCoverage.Validator();
        var query = new GetServiceReliabilityCoverage.Query("");

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }
}
