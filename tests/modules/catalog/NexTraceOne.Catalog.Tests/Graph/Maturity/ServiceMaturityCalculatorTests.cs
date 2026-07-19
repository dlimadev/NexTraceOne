using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Maturity;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Tests.Graph.Maturity;

/// <summary>
/// Testes unitários para ServiceMaturityCalculator.
/// Cobre Compute (função pura, sem repos) com serviço completo e serviço vazio.
/// </summary>
public sealed class ServiceMaturityCalculatorTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ServiceAsset CreateFullService()
    {
        var svc = ServiceAsset.Create("svc-full", "payments", "payments-team", Guid.NewGuid());
        svc.UpdateOwnership("payments-team", "alice", "bob");
        svc.UpdateDetails(
            "Full Service",
            "Serviço com todos os sinais preenchidos.",
            ServiceType.RestApi,
            "core",
            Criticality.Medium,
            LifecycleStatus.Active,
            ExposureType.Internal,
            "https://docs.example.com",
            "https://github.com/example/svc-full");
        return svc;
    }

    private static ServiceAsset CreateEmptyService()
        => ServiceAsset.Create("svc-empty", "unknown", "lost-team", Guid.NewGuid());

    private static ServiceMaturityCalculator BuildCalculator() => new(
        Substitute.For<IServiceLinkRepository>(),
        Substitute.For<IApiAssetRepository>(),
        Substitute.For<IContractVersionRepository>());

    // ── Compute — serviço completo ────────────────────────────────────────────

    [Fact]
    public void Compute_FullServiceWithAllSignals_ReturnsHighLevelAndScore()
    {
        // Arrange
        var svc = CreateFullService();
        var links = new List<ServiceLink>
        {
            ServiceLink.Create(svc.Id, LinkCategory.Runbook, "Runbook Principal", "https://runbook.example.com"),
            ServiceLink.Create(svc.Id, LinkCategory.Monitoring, "Grafana", "https://grafana.example.com"),
        };
        var api = ApiAsset.Register("api-v1", "/api/v1", "1.0", "Internal", svc);
        var apis = new List<ApiAsset> { api };
        var calculator = BuildCalculator();

        // Act
        var result = calculator.Compute(svc, links, apis, contractCount: 1);

        // Assert — nível mínimo "Managed" (score ≥ 0.7)
        result.OverallScore.Should().BeGreaterThanOrEqualTo(0.7m);
        result.Level.Should().BeOneOf("Managed", "Optimizing");
        result.HasOwnership.Should().BeTrue();
        result.HasContracts.Should().BeTrue();
        result.HasDocumentation.Should().BeTrue();
        result.HasRunbook.Should().BeTrue();
        result.HasMonitoring.Should().BeTrue();
        result.HasRepository.Should().BeTrue();
        result.ApiCount.Should().Be(1);
        result.ContractCount.Should().Be(1);
        result.LinkCount.Should().Be(2);
    }

    // ── Compute — serviço vazio ───────────────────────────────────────────────

    [Fact]
    public void Compute_EmptyService_ReturnsInitialLevel()
    {
        // Arrange — sem owner técnico, sem docs, sem links, sem contratos, sem APIs
        var svc = CreateEmptyService();
        var calculator = BuildCalculator();

        // Act
        var result = calculator.Compute(svc, [], [], contractCount: 0);

        // Assert
        result.Level.Should().Be("Initial");
        result.OverallScore.Should().BeLessThan(0.25m);
        result.HasOwnership.Should().BeFalse();
        result.HasContracts.Should().BeFalse();
        result.HasDocumentation.Should().BeFalse();
        result.HasRunbook.Should().BeFalse();
        result.HasMonitoring.Should().BeFalse();
        result.ApiCount.Should().Be(0);
        result.ContractCount.Should().Be(0);
        result.LinkCount.Should().Be(0);
    }

    // ── Compute — limites de nível ────────────────────────────────────────────

    [Fact]
    public void Compute_ScoreAt90_ReturnsOptimizing()
    {
        // Arrange — todos os sinais activos (score esperado = 1.0)
        var svc = CreateFullService();
        var links = new List<ServiceLink>
        {
            ServiceLink.Create(svc.Id, LinkCategory.Runbook, "Runbook", "https://rb"),
            ServiceLink.Create(svc.Id, LinkCategory.Monitoring, "Monitor", "https://mon"),
        };
        var api = ApiAsset.Register("api-v1", "/api/v1", "1.0", "Internal", svc);
        var calculator = BuildCalculator();

        // Act
        var result = calculator.Compute(svc, links, [api], contractCount: 2);

        // Assert
        result.Level.Should().Be("Optimizing");
        result.OverallScore.Should().Be(1.0m);
    }

    [Fact]
    public void Compute_OverallScore_IsRoundedToTwoDecimals()
    {
        // Arrange — sem links, com team mas sem TechnicalOwner → partial ownership
        var svc = ServiceAsset.Create("svc-partial", "platform", "team-a", Guid.NewGuid());
        var calculator = BuildCalculator();

        // Act
        var result = calculator.Compute(svc, [], [], contractCount: 0);

        // Assert — score deve estar entre 0 e 1, com 2 casas decimais
        result.OverallScore.Should().BeGreaterThanOrEqualTo(0m).And.BeLessThanOrEqualTo(1m);
        var roundTrip = Math.Round(result.OverallScore, 2);
        result.OverallScore.Should().Be(roundTrip);
    }
}
