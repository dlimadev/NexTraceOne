using NexTraceOne.Governance.Application.Features.GetDomainFinOps;
using NexTraceOne.Governance.Application.Features.GetEfficiencyIndicators;
using NexTraceOne.Governance.Application.Features.GetFinOpsSummary;
using NexTraceOne.Governance.Application.Features.GetFinOpsTrends;
using NexTraceOne.Governance.Application.Features.GetServiceFinOps;
using NexTraceOne.Governance.Application.Features.GetTeamFinOps;
using NexTraceOne.Governance.Application.Features.GetWasteSignals;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para as features de FinOps contextual.
/// Handlers sem dependências que retornam dados demonstrativos.
/// </summary>
public sealed class FinOpsFeatureTests
{
    // ── GetFinOpsSummary ──

    [Fact]
    public async Task GetFinOpsSummary_ShouldReturnServiceCosts()
    {
        // Arrange
        var handler = new GetFinOpsSummary.Handler();
        var query = new GetFinOpsSummary.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Services.Should().NotBeEmpty();
        result.Value.TotalMonthlyCost.Should().BeGreaterThan(0);
        result.Value.IsSimulated.Should().BeTrue();
        result.Value.DataSource.Should().Be("demo");
    }

    [Fact]
    public async Task GetFinOpsSummary_ShouldReturnTopCostDriversAndWasteSignals()
    {
        // Arrange
        var handler = new GetFinOpsSummary.Handler();

        // Act
        var result = await handler.Handle(new GetFinOpsSummary.Query(), CancellationToken.None);

        // Assert
        result.Value.TopCostDrivers.Should().NotBeEmpty();
        result.Value.TopWasteSignals.Should().NotBeEmpty();
        result.Value.OptimizationOpportunities.Should().NotBeEmpty();
    }

    // ── GetFinOpsTrends ──

    [Fact]
    public async Task GetFinOpsTrends_ShouldReturnTrendSeries()
    {
        // Arrange
        var handler = new GetFinOpsTrends.Handler();
        var query = new GetFinOpsTrends.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Series.Should().NotBeEmpty();
        result.Value.AggregatedTrend.Should().NotBeEmpty();
        result.Value.IsSimulated.Should().BeTrue();
    }

    // ── GetDomainFinOps ──

    [Fact]
    public async Task GetDomainFinOps_ShouldReturnDomainProfile()
    {
        // Arrange
        var handler = new GetDomainFinOps.Handler();
        var query = new GetDomainFinOps.Query("domain-commerce");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DomainId.Should().Be("domain-commerce");
        result.Value.Teams.Should().NotBeEmpty();
        result.Value.TotalMonthlyCost.Should().BeGreaterThan(0);
        result.Value.IsSimulated.Should().BeTrue();
    }

    // ── GetServiceFinOps ──

    [Fact]
    public async Task GetServiceFinOps_ShouldReturnServiceProfile()
    {
        // Arrange
        var handler = new GetServiceFinOps.Handler();
        var query = new GetServiceFinOps.Query("svc-payment-api");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceId.Should().Be("svc-payment-api");
        result.Value.MonthlyCost.Should().BeGreaterThan(0);
        result.Value.WasteSignals.Should().NotBeEmpty();
        result.Value.EfficiencyIndicators.Should().NotBeEmpty();
        result.Value.ChangeImpacts.Should().NotBeEmpty();
        result.Value.Optimizations.Should().NotBeEmpty();
    }

    // ── GetTeamFinOps ──

    [Fact]
    public async Task GetTeamFinOps_ShouldReturnTeamProfile()
    {
        // Arrange
        var handler = new GetTeamFinOps.Handler();
        var query = new GetTeamFinOps.Query("team-commerce");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TeamId.Should().Be("team-commerce");
        result.Value.Services.Should().NotBeEmpty();
        result.Value.TrendSeries.Should().NotBeEmpty();
        result.Value.TotalMonthlyCost.Should().BeGreaterThan(0);
        result.Value.IsSimulated.Should().BeTrue();
    }

    // ── GetWasteSignals ──

    [Fact]
    public async Task GetWasteSignals_ShouldReturnSignals()
    {
        // Arrange
        var handler = new GetWasteSignals.Handler();
        var query = new GetWasteSignals.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Signals.Should().NotBeEmpty();
        result.Value.TotalWaste.Should().BeGreaterThan(0);
        result.Value.ByType.Should().NotBeEmpty();
        result.Value.IsSimulated.Should().BeTrue();
    }

    [Fact]
    public async Task GetWasteSignals_SignalsShouldHaveRequiredFields()
    {
        // Arrange
        var handler = new GetWasteSignals.Handler();

        // Act
        var result = await handler.Handle(new GetWasteSignals.Query(), CancellationToken.None);

        // Assert
        result.Value.Signals.Should().AllSatisfy(s =>
        {
            s.SignalId.Should().NotBeNullOrWhiteSpace();
            s.ServiceId.Should().NotBeNullOrWhiteSpace();
            s.EstimatedWaste.Should().BeGreaterThan(0);
        });
    }

    // ── GetEfficiencyIndicators ──

    [Fact]
    public async Task GetEfficiencyIndicators_ShouldReturnIndicators()
    {
        // Arrange
        var handler = new GetEfficiencyIndicators.Handler();
        var query = new GetEfficiencyIndicators.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Services.Should().NotBeEmpty();
        result.Value.OverallEfficiencyScore.Should().BeGreaterThan(0);
        result.Value.IsSimulated.Should().BeTrue();
    }

    [Fact]
    public async Task GetEfficiencyIndicators_ServicesShouldHaveMetrics()
    {
        // Arrange
        var handler = new GetEfficiencyIndicators.Handler();

        // Act
        var result = await handler.Handle(new GetEfficiencyIndicators.Query(), CancellationToken.None);

        // Assert
        result.Value.Services.Should().AllSatisfy(s =>
        {
            s.ServiceId.Should().NotBeNullOrWhiteSpace();
            s.Metrics.Should().NotBeEmpty();
        });
    }
}
