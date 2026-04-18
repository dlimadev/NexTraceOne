using NexTraceOne.Governance.Application.Features.GetDomainFinOps;
using NexTraceOne.Governance.Application.Features.GetEfficiencyIndicators;
using NexTraceOne.Governance.Application.Features.GetFinOpsSummary;
using NexTraceOne.Governance.Application.Features.GetFinOpsTrends;
using NexTraceOne.Governance.Application.Features.GetServiceFinOps;
using NexTraceOne.Governance.Application.Features.GetTeamFinOps;
using NexTraceOne.Governance.Application.Features.GetWasteSignals;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Contracts.Reliability.ServiceInterfaces;
using NSubstitute;
using System.Linq;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para as features de FinOps contextual.
/// Handlers consomem dados reais do módulo CostIntelligence via mock.
/// </summary>
public sealed class FinOpsFeatureTests
{
    private static readonly CostRecordSummary[] SampleRecords =
    [
        new("svc-payment-api", "Payment API", "Team Payments", "Payments", "Production", 12500m, "EUR", "2026-03", "azure"),
        new("svc-order-processor", "Order Processor", "Team Commerce", "Commerce", "Production", 18700m, "EUR", "2026-03", "azure"),
        new("svc-user-service", "User Service", "Team Identity", "Identity", "Production", 4200m, "EUR", "2026-03", "azure")
    ];

    private static ICostIntelligenceModule CreateMock()
    {
        var mock = Substitute.For<ICostIntelligenceModule>();
        mock.GetCostRecordsAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(SampleRecords);
        mock.GetServiceCostAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var id = callInfo.ArgAt<string>(0);
                return SampleRecords.FirstOrDefault(r => r.ServiceId.Equals(id, StringComparison.OrdinalIgnoreCase));
            });
        mock.GetCostsByTeamAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var team = callInfo.ArgAt<string>(0);
                return (IReadOnlyList<CostRecordSummary>)SampleRecords
                    .Where(r => (r.Team ?? string.Empty).Equals(team, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            });
        mock.GetCostsByDomainAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var domain = callInfo.ArgAt<string>(0);
                return (IReadOnlyList<CostRecordSummary>)SampleRecords
                    .Where(r => (r.Domain ?? string.Empty).Equals(domain, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            });
        return mock;
    }

    private static IReliabilityModule CreateReliabilityMock()
    {
        var mock = Substitute.For<IReliabilityModule>();
        mock.GetRemainingErrorBudgetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(0.85m);
        mock.GetCurrentBurnRateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(0.7m);
        mock.GetCurrentReliabilityStatusAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Healthy");
        return mock;
    }

    // ── GetFinOpsSummary ──

    [Fact]
    public async Task GetFinOpsSummary_ShouldReturnServiceCosts()
    {
        // Arrange
        var handler = new GetFinOpsSummary.Handler(CreateMock());
        var query = new GetFinOpsSummary.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Services.Should().NotBeEmpty();
        result.Value.TotalMonthlyCost.Should().BeGreaterThan(0);
        result.Value.IsSimulated.Should().BeFalse();
        result.Value.DataSource.Should().Be("cost-intelligence");
    }

    [Fact]
    public async Task GetFinOpsSummary_ShouldReturnTopCostDrivers()
    {
        // Arrange
        var handler = new GetFinOpsSummary.Handler(CreateMock());

        // Act
        var result = await handler.Handle(new GetFinOpsSummary.Query(), CancellationToken.None);

        // Assert
        result.Value.TopCostDrivers.Should().NotBeEmpty();
        result.Value.OptimizationOpportunities.Should().NotBeEmpty();
    }

    // ── GetFinOpsTrends ──

    [Fact]
    public async Task GetFinOpsTrends_ShouldReturnTrendSeries()
    {
        // Arrange
        var handler = new GetFinOpsTrends.Handler(CreateMock());
        var query = new GetFinOpsTrends.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Series.Should().NotBeEmpty();
        result.Value.AggregatedTrend.Should().NotBeEmpty();
        result.Value.IsSimulated.Should().BeFalse();
    }

    // ── GetDomainFinOps ──

    [Fact]
    public async Task GetDomainFinOps_ShouldReturnDomainProfile()
    {
        // Arrange
        var handler = new GetDomainFinOps.Handler(CreateMock());
        var query = new GetDomainFinOps.Query("Commerce");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DomainId.Should().Be("Commerce");
        result.Value.Teams.Should().NotBeEmpty();
        result.Value.TotalMonthlyCost.Should().BeGreaterThan(0);
        result.Value.IsSimulated.Should().BeFalse();
    }

    // ── GetServiceFinOps ──

    [Fact]
    public async Task GetServiceFinOps_ShouldReturnServiceProfile()
    {
        // Arrange
        var handler = new GetServiceFinOps.Handler(CreateMock(), CreateReliabilityMock());
        var query = new GetServiceFinOps.Query("svc-payment-api");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceId.Should().Be("svc-payment-api");
        result.Value.MonthlyCost.Should().BeGreaterThan(0);
        result.Value.IsSimulated.Should().BeFalse();
        result.Value.DataSource.Should().Be("cost-intelligence+reliability");
        result.Value.TotalWaste.Should().BeGreaterThan(0);
        result.Value.Optimizations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetServiceFinOps_ShouldReturnNotFoundWhenNoData()
    {
        // Arrange
        var mock = Substitute.For<ICostIntelligenceModule>();
        mock.GetServiceCostAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((CostRecordSummary?)null);

        var handler = new GetServiceFinOps.Handler(mock, CreateReliabilityMock());
        var query = new GetServiceFinOps.Query("svc-nonexistent");
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    // ── GetTeamFinOps ──

    [Fact]
    public async Task GetTeamFinOps_ShouldReturnTeamProfile()
    {
        // Arrange
        var handler = new GetTeamFinOps.Handler(CreateMock());
        var query = new GetTeamFinOps.Query("Team Commerce");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TeamId.Should().Be("Team Commerce");
        result.Value.Services.Should().NotBeEmpty();
        result.Value.TotalMonthlyCost.Should().BeGreaterThan(0);
        result.Value.IsSimulated.Should().BeFalse();
        result.Value.TrendSeries.Should().NotBeEmpty();
        result.Value.TotalWaste.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetTeamFinOps_ShouldReturnEmptyWhenNoData()
    {
        // Arrange
        var mock = Substitute.For<ICostIntelligenceModule>();
        mock.GetCostsByTeamAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CostRecordSummary>());

        var handler = new GetTeamFinOps.Handler(mock);
        var query = new GetTeamFinOps.Query("nonexistent-team");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceCount.Should().Be(0);
        result.Value.TotalMonthlyCost.Should().Be(0);
    }

    // ── GetWasteSignals ──

    [Fact]
    public async Task GetWasteSignals_ShouldReturnSignalsForHighCostServices()
    {
        // Arrange
        var handler = new GetWasteSignals.Handler(CreateMock());
        var query = new GetWasteSignals.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsSimulated.Should().BeFalse("handler now uses real cost data");
        result.Value.DataSource.Should().Be("cost-intelligence");
        result.Value.SignalCount.Should().BeGreaterThan(0);
        result.Value.TotalWaste.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetDomainFinOps_ShouldExposeTrendAndWasteFromRealRecords()
    {
        // Arrange
        var handler = new GetDomainFinOps.Handler(CreateMock());

        // Act
        var result = await handler.Handle(new GetDomainFinOps.Query("Payments"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TrendSeries.Should().NotBeEmpty();
        result.Value.IsSimulated.Should().BeFalse();
        result.Value.DataSource.Should().Be("cost-intelligence");
    }

    [Fact]
    public async Task GetWasteSignals_ShouldReturnEmptyWhenNoCostData()
    {
        // Arrange
        var mock = Substitute.For<ICostIntelligenceModule>();
        mock.GetCostRecordsAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CostRecordSummary>());

        var handler = new GetWasteSignals.Handler(mock);
        var query = new GetWasteSignals.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Signals.Should().BeEmpty();
        result.Value.TotalWaste.Should().Be(0);
        result.Value.IsSimulated.Should().BeFalse();
    }

    [Fact]
    public async Task GetWasteSignals_SignalsShouldHaveRequiredFields()
    {
        // Arrange
        var handler = new GetWasteSignals.Handler(CreateMock());

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
    public async Task GetEfficiencyIndicators_ShouldReturnIndicatorsFromRealData()
    {
        // Arrange
        var handler = new GetEfficiencyIndicators.Handler(CreateMock());
        var query = new GetEfficiencyIndicators.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Services.Should().NotBeEmpty();
        result.Value.OverallEfficiencyScore.Should().BeGreaterThan(0);
        result.Value.IsSimulated.Should().BeFalse("handler now uses real cost data");
        result.Value.DataSource.Should().Be("cost-intelligence");
    }

    [Fact]
    public async Task GetEfficiencyIndicators_ShouldReturnEmptyWhenNoCostData()
    {
        // Arrange
        var mock = Substitute.For<ICostIntelligenceModule>();
        mock.GetCostRecordsAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CostRecordSummary>());

        var handler = new GetEfficiencyIndicators.Handler(mock);
        var query = new GetEfficiencyIndicators.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Services.Should().BeEmpty();
        result.Value.OverallEfficiencyScore.Should().Be(0);
        result.Value.IsSimulated.Should().BeFalse();
    }

    [Fact]
    public async Task GetEfficiencyIndicators_ServicesShouldHaveMetrics()
    {
        // Arrange
        var handler = new GetEfficiencyIndicators.Handler(CreateMock());

        // Act
        var result = await handler.Handle(new GetEfficiencyIndicators.Query(), CancellationToken.None);

        // Assert
        result.Value.Services.Should().AllSatisfy(s =>
        {
            s.ServiceId.Should().NotBeNullOrWhiteSpace();
            s.Metrics.Should().NotBeEmpty();
        });
    }

    // ── GetFinOpsSummary — PotentialSavings ──

    [Fact]
    public async Task GetFinOpsSummary_OptimizationOpportunities_ShouldHaveNonZeroPotentialSavings()
    {
        // Arrange: svc-order-processor (18700 EUR) e svc-payment-api (12500 EUR) são Wasteful/Inefficient
        var handler = new GetFinOpsSummary.Handler(CreateMock());

        // Act
        var result = await handler.Handle(new GetFinOpsSummary.Query(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OptimizationOpportunities.Should().NotBeEmpty();
        result.Value.OptimizationOpportunities.Should().AllSatisfy(opt =>
            opt.PotentialSavings.Should().BeGreaterThan(0,
                "potentialSavings deve ser derivado do desperdício calculado (waste * 0.35), nunca hardcoded a 0"));
    }

    [Fact]
    public async Task GetFinOpsSummary_OptimizationOpportunities_ShouldPrioritizeWastefulAsHigh()
    {
        // Arrange
        var handler = new GetFinOpsSummary.Handler(CreateMock());

        // Act
        var result = await handler.Handle(new GetFinOpsSummary.Query(), CancellationToken.None);

        // Assert: svc-order-processor (18700) é Wasteful → High priority
        var wastefulOpt = result.Value.OptimizationOpportunities
            .FirstOrDefault(opt => opt.ServiceId == "svc-order-processor");

        wastefulOpt.Should().NotBeNull();
        wastefulOpt!.Priority.Should().Be("High");
    }

    [Fact]
    public async Task GetFinOpsSummary_OptimizationOpportunities_ShouldNotIncludeEfficientServices()
    {
        // Arrange
        var handler = new GetFinOpsSummary.Handler(CreateMock());

        // Act
        var result = await handler.Handle(new GetFinOpsSummary.Query(), CancellationToken.None);

        // Assert: svc-user-service (4200 EUR) é Efficient — não deve aparecer em OptimizationOpportunities
        var efficientOpt = result.Value.OptimizationOpportunities
            .FirstOrDefault(opt => opt.ServiceId == "svc-user-service");

        efficientOpt.Should().BeNull("serviços Efficient não geram oportunidades de otimização");
    }

    [Fact]
    public async Task GetFinOpsSummary_EmptyRecords_ShouldReturnEmptyOptimizationOpportunities()
    {
        // Arrange
        var mock = Substitute.For<ICostIntelligenceModule>();
        mock.GetCostRecordsAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CostRecordSummary>());

        var handler = new GetFinOpsSummary.Handler(mock);

        // Act
        var result = await handler.Handle(new GetFinOpsSummary.Query(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OptimizationOpportunities.Should().BeEmpty();
    }
}
