using NexTraceOne.Governance.Application.Features.ApplyGovernancePack;
using NexTraceOne.Governance.Application.Features.GetDomainGovernanceSummary;
using NexTraceOne.Governance.Application.Features.GetPackApplicability;
using NexTraceOne.Governance.Application.Features.GetPackCoverage;
using NexTraceOne.Governance.Application.Features.GetTeamGovernanceSummary;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para features de resumo executivo de governança.
/// Handlers sem dependências que retornam dados estáticos/demonstrativos.
/// </summary>
public sealed class ExecutiveGovernanceSummaryTests
{
    // ── GetDomainGovernanceSummary ──

    [Fact]
    public async Task GetDomainGovernanceSummary_ShouldReturnDimensions()
    {
        // Arrange
        var handler = new GetDomainGovernanceSummary.Handler();
        var query = new GetDomainGovernanceSummary.Query("domain-commerce");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DomainId.Should().Be("domain-commerce");
        result.Value.Dimensions.Should().NotBeEmpty();
        result.Value.OverallMaturity.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetDomainGovernanceSummary_DimensionsShouldHaveScoresAndTrends()
    {
        // Arrange
        var handler = new GetDomainGovernanceSummary.Handler();

        // Act
        var result = await handler.Handle(new GetDomainGovernanceSummary.Query("test"), CancellationToken.None);

        // Assert
        result.Value.Dimensions.Should().AllSatisfy(d =>
        {
            d.Dimension.Should().NotBeNullOrWhiteSpace();
            d.Level.Should().NotBeNullOrWhiteSpace();
            d.Score.Should().BeGreaterThan(0);
            d.Trend.Should().NotBeNullOrWhiteSpace();
        });
    }

    // ── GetTeamGovernanceSummary ──

    [Fact]
    public async Task GetTeamGovernanceSummary_ShouldReturnDimensions()
    {
        // Arrange
        var handler = new GetTeamGovernanceSummary.Handler();
        var query = new GetTeamGovernanceSummary.Query("team-commerce");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TeamId.Should().Be("team-commerce");
        result.Value.Dimensions.Should().NotBeEmpty();
        result.Value.OverallMaturity.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetTeamGovernanceSummary_ShouldReturnCoverageMetrics()
    {
        // Arrange
        var handler = new GetTeamGovernanceSummary.Handler();

        // Act
        var result = await handler.Handle(new GetTeamGovernanceSummary.Query("team-test"), CancellationToken.None);

        // Assert
        result.Value.OwnershipCoverage.Should().BeGreaterThan(0);
        result.Value.ContractCoverage.Should().BeGreaterThan(0);
        result.Value.DocumentationCoverage.Should().BeGreaterThan(0);
        result.Value.ReliabilityScore.Should().BeGreaterThan(0);
    }

    // ── ApplyGovernancePack ──

    [Fact]
    public async Task ApplyGovernancePack_ShouldReturnRolloutId()
    {
        // Arrange
        var handler = new ApplyGovernancePack.Handler();
        var command = new ApplyGovernancePack.Command(
            Guid.NewGuid().ToString(), "Domain", "payments", "Blocking", "admin@company.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RolloutId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(result.Value.RolloutId, out _).Should().BeTrue();
    }

    // ── GetPackApplicability ──

    [Fact]
    public async Task GetPackApplicability_ShouldReturnScopes()
    {
        // Arrange
        var handler = new GetPackApplicability.Handler();
        var query = new GetPackApplicability.Query(Guid.NewGuid().ToString());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Scopes.Should().NotBeEmpty();
        result.Value.Scopes.Should().AllSatisfy(s =>
        {
            s.AppliedBy.Should().NotBeNullOrWhiteSpace();
            s.ScopeValue.Should().NotBeNullOrWhiteSpace();
        });
    }

    // ── GetPackCoverage ──

    [Fact]
    public async Task GetPackCoverage_ShouldReturnCoverageItems()
    {
        // Arrange
        var handler = new GetPackCoverage.Handler();
        var query = new GetPackCoverage.Query(Guid.NewGuid().ToString());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().NotBeEmpty();
        result.Value.OverallCoveragePercent.Should().BeGreaterThan(0);
        result.Value.TotalScopes.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetPackCoverage_ItemsShouldHaveConsistentCounts()
    {
        // Arrange
        var handler = new GetPackCoverage.Handler();

        // Act
        var result = await handler.Handle(new GetPackCoverage.Query("test"), CancellationToken.None);

        // Assert
        result.Value.Items.Should().AllSatisfy(item =>
        {
            (item.CompliantCount + item.NonCompliantCount).Should().Be(item.TotalRules);
            item.CoveragePercent.Should().BeGreaterThanOrEqualTo(0);
            item.CoveragePercent.Should().BeLessThanOrEqualTo(100);
        });
    }
}
