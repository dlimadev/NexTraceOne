using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.Features.GetAdoptionFunnel;
using NexTraceOne.ProductAnalytics.Application.Features.GetFeatureHeatmap;
using NexTraceOne.ProductAnalytics.Application.Features.GetFrictionIndicators;
using NexTraceOne.ProductAnalytics.Application.Features.GetModuleAdoption;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Tests.Application.Features;

/// <summary>
/// Testes avançados de unidade para GetAdoptionFunnel, GetFeatureHeatmap,
/// GetFrictionIndicators e GetModuleAdoption — cobrindo cenários com dados,
/// sem dados, filtros e cálculos de métricas.
/// </summary>
public sealed class AdvancedAnalyticsFeatureTests
{
    private readonly IAnalyticsEventRepository _repo = Substitute.For<IAnalyticsEventRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private static readonly DateTimeOffset Now = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    public AdvancedAnalyticsFeatureTests()
    {
        _clock.UtcNow.Returns(Now);
    }

    // ═══════════════════════════════════════════════════════════════════
    // GetAdoptionFunnel
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAdoptionFunnel_WithSessionData_ShouldComputeStepsAndCompletion()
    {
        // Arrange — 3 sessions through ContractStudio funnel:
        //   session-1: ModuleViewed + ContractDraftCreated + ContractPublished (full)
        //   session-2: ModuleViewed + ContractDraftCreated (partial)
        //   session-3: ModuleViewed only (entry only)
        var sessionEvents = new List<SessionEventTypeRow>
        {
            new("session-1", AnalyticsEventType.ModuleViewed, Now.AddHours(-2)),
            new("session-1", AnalyticsEventType.ContractDraftCreated, Now.AddHours(-1)),
            new("session-1", AnalyticsEventType.ContractPublished, Now.AddMinutes(-30)),
            new("session-2", AnalyticsEventType.ModuleViewed, Now.AddHours(-3)),
            new("session-2", AnalyticsEventType.ContractDraftCreated, Now.AddHours(-2)),
            new("session-3", AnalyticsEventType.ModuleViewed, Now.AddHours(-4)),
        };

        _repo.GetSessionEventTypesAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(sessionEvents);

        var handler = new GetAdoptionFunnel.Handler(_repo, _clock);
        var query = new GetAdoptionFunnel.Query(null, null, null, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Funnels.Should().HaveCount(6);
        result.Value.PeriodLabel.Should().Be("last_30d");

        var contractFunnel = result.Value.Funnels.First(f => f.Module == ProductModule.ContractStudio);
        contractFunnel.TotalSessions.Should().Be(3);
        contractFunnel.Steps.Should().HaveCount(3);
        contractFunnel.Steps[0].SessionCount.Should().Be(3);
        contractFunnel.Steps[0].CompletionPercent.Should().Be(100m);
        contractFunnel.Steps[1].SessionCount.Should().Be(2);
        contractFunnel.Steps[2].SessionCount.Should().Be(1);
        contractFunnel.CompletionRate.Should().Be(contractFunnel.Steps[^1].CompletionPercent);
    }

    [Fact]
    public async Task GetAdoptionFunnel_WithNoSessionData_ShouldReturnFunnelsWithZeroSessions()
    {
        // Arrange
        _repo.GetSessionEventTypesAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionEventTypeRow>());

        var handler = new GetAdoptionFunnel.Handler(_repo, _clock);

        // Act
        var result = await handler.Handle(new GetAdoptionFunnel.Query(null, null, null, null), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Funnels.Should().HaveCount(6);
        result.Value.Funnels.Should().OnlyContain(f => f.TotalSessions == 0);
        result.Value.Funnels.Should().OnlyContain(f => f.CompletionRate == 0m);
    }

    [Fact]
    public async Task GetAdoptionFunnel_WithModuleFilter_ShouldReturnOnlyMatchingModule()
    {
        // Arrange
        _repo.GetSessionEventTypesAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionEventTypeRow>
            {
                new("s1", AnalyticsEventType.ModuleViewed, Now.AddHours(-1)),
                new("s1", AnalyticsEventType.AssistantPromptSubmitted, Now.AddMinutes(-30)),
            });

        var handler = new GetAdoptionFunnel.Handler(_repo, _clock);
        var query = new GetAdoptionFunnel.Query(Module: "AiAssistant", Persona: null, TeamId: null, Range: null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Funnels.Should().HaveCount(1);
        result.Value.Funnels[0].Module.Should().Be(ProductModule.AiAssistant);
        result.Value.Funnels[0].TotalSessions.Should().Be(1);
    }

    [Fact]
    public async Task GetAdoptionFunnel_WithCustomRange_ShouldUsePeriodLabel()
    {
        // Arrange
        _repo.GetSessionEventTypesAsync(
            Arg.Any<AnalyticsEventType[]>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionEventTypeRow>());

        var handler = new GetAdoptionFunnel.Handler(_repo, _clock);

        // Act
        var result = await handler.Handle(
            new GetAdoptionFunnel.Query(null, null, null, Range: "last_7d"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PeriodLabel.Should().Be("last_7d");
    }

    // ═══════════════════════════════════════════════════════════════════
    // GetFeatureHeatmap
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetFeatureHeatmap_WithNoAdoptionData_ShouldReturnEmptyCells()
    {
        // Arrange
        _repo.GetModuleAdoptionAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleAdoptionRow>());
        _repo.GetFeatureCountsAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleFeatureCountRow>());
        _repo.CountUniqueUsersAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var handler = new GetFeatureHeatmap.Handler(_repo, _clock);

        // Act
        var result = await handler.Handle(new GetFeatureHeatmap.Query(null, null, null), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Cells.Should().BeEmpty();
        result.Value.Modules.Should().BeEmpty();
        result.Value.MaxIntensity.Should().Be(0m);
        result.Value.TotalUniqueUsers.Should().Be(0);
    }

    [Fact]
    public async Task GetFeatureHeatmap_WithDataAndFeatures_ShouldComputeIntensityAndTopFeatures()
    {
        // Arrange — two modules: ServiceCatalog (high usage) and AiAssistant (low usage)
        _repo.GetModuleAdoptionAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleAdoptionRow>
            {
                new(ProductModule.ServiceCatalog, 500, 40),
                new(ProductModule.AiAssistant, 100, 10),
            });
        _repo.GetFeatureCountsAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleFeatureCountRow>
            {
                new(ProductModule.ServiceCatalog, "list", 200),
                new(ProductModule.ServiceCatalog, "detail", 150),
                new(ProductModule.AiAssistant, "prompt", 80),
            });
        _repo.CountUniqueUsersAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(50);

        var handler = new GetFeatureHeatmap.Handler(_repo, _clock);

        // Act
        var result = await handler.Handle(new GetFeatureHeatmap.Query(null, null, null), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Cells.Should().HaveCount(2);
        result.Value.TotalUniqueUsers.Should().Be(50);
        result.Value.MaxIntensity.Should().Be(100m);

        // Cells should be ordered by intensity descending — ServiceCatalog first
        result.Value.Cells[0].Module.Should().Be(ProductModule.ServiceCatalog);
        result.Value.Cells[0].Intensity.Should().Be(100m);
        result.Value.Cells[0].AdoptionPercent.Should().Be(80); // 40/50 * 100 = 80
        result.Value.Cells[0].TopFeatures.Should().HaveCount(2);

        result.Value.Cells[1].Module.Should().Be(ProductModule.AiAssistant);
        result.Value.Cells[1].Intensity.Should().Be(20m); // 100/500 * 100 = 20
        result.Value.Cells[1].AdoptionPercent.Should().Be(20); // 10/50 * 100 = 20
    }

    [Fact]
    public async Task GetFeatureHeatmap_WithPersonaFilter_ShouldPassPersonaToRepository()
    {
        // Arrange
        _repo.GetModuleAdoptionAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleAdoptionRow>
            {
                new(ProductModule.Governance, 60, 5),
            });
        _repo.GetFeatureCountsAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleFeatureCountRow>());
        _repo.CountUniqueUsersAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(5);

        var handler = new GetFeatureHeatmap.Handler(_repo, _clock);

        // Act
        var result = await handler.Handle(
            new GetFeatureHeatmap.Query(Persona: "Architect", TeamId: null, Range: "last_90d"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Cells.Should().HaveCount(1);
        result.Value.PeriodLabel.Should().Be("last_90d");

        await _repo.Received(1).GetModuleAdoptionAsync(
            "Architect", Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    // ═══════════════════════════════════════════════════════════════════
    // GetFrictionIndicators
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetFrictionIndicators_WithZeroTotalEvents_ShouldReturnEmptyIndicators()
    {
        // Arrange
        _repo.CountAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0L);

        var handler = new GetFrictionIndicators.Handler(_repo, _clock);

        // Act
        var result = await handler.Handle(
            new GetFrictionIndicators.Query(null, null, null), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Indicators.Should().BeEmpty();
        result.Value.OverallFrictionScore.Should().Be(0m);
        result.Value.ImprovingSignals.Should().Be(0);
        result.Value.DecliningSignals.Should().Be(0);
        result.Value.StableSignals.Should().Be(0);
        result.Value.IsSimulated.Should().BeFalse();
        result.Value.DataSource.Should().Be("analytics");
    }

    [Fact]
    public async Task GetFrictionIndicators_WithModuleFilterMatchingNone_ShouldReturnEmptyIndicators()
    {
        // Arrange — indicators all point to ServiceCatalog, filter by ChangeIntelligence
        _repo.CountAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(500L);
        _repo.CountByEventTypeAsync(
            Arg.Any<AnalyticsEventType>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(50L);
        _repo.GetTopModulesAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleUsageRow> { new(ProductModule.ServiceCatalog, 100, 20) });

        var handler = new GetFrictionIndicators.Handler(_repo, _clock);

        // Act — filter by ChangeIntelligence which none of the indicators point to
        var result = await handler.Handle(
            new GetFrictionIndicators.Query(null, Module: "ChangeIntelligence", null), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Indicators.Should().BeEmpty();
        result.Value.OverallFrictionScore.Should().Be(0m);
    }

    [Fact]
    public async Task GetFrictionIndicators_TrendImproving_WhenCurrentLessThanPrevious()
    {
        // Arrange — current period count much lower than previous → Improving trend
        _repo.CountAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(1000L);

        // Previous period: from < Now-30d; current period: from >= Now-30d
        _repo.CountByEventTypeAsync(
            Arg.Any<AnalyticsEventType>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var from = callInfo.ArgAt<DateTimeOffset>(2);
                return from < Now.AddDays(-31) ? 100L : 10L;
            });
        _repo.GetTopModulesAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleUsageRow> { new(ProductModule.Dashboard, 50, 10) });

        var handler = new GetFrictionIndicators.Handler(_repo, _clock);

        // Act
        var result = await handler.Handle(
            new GetFrictionIndicators.Query(null, null, null), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Indicators.Should().NotBeEmpty();
        result.Value.ImprovingSignals.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetFrictionIndicators_TrendDeclining_WhenCurrentGreaterThanPrevious()
    {
        // Arrange — current period count much higher than previous → Declining trend
        _repo.CountAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(1000L);

        // Previous period: from < Now-30d; current period: from >= Now-30d
        _repo.CountByEventTypeAsync(
            Arg.Any<AnalyticsEventType>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var from = callInfo.ArgAt<DateTimeOffset>(2);
                return from < Now.AddDays(-31) ? 10L : 100L;
            });
        _repo.GetTopModulesAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleUsageRow> { new(ProductModule.Dashboard, 50, 10) });

        var handler = new GetFrictionIndicators.Handler(_repo, _clock);

        // Act
        var result = await handler.Handle(
            new GetFrictionIndicators.Query(null, null, null), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Indicators.Should().NotBeEmpty();
        result.Value.DecliningSignals.Should().BeGreaterThan(0);
    }

    // ═══════════════════════════════════════════════════════════════════
    // GetModuleAdoption
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetModuleAdoption_WithNoData_ShouldReturnEmptyModulesAndZeroScore()
    {
        // Arrange
        _repo.GetModuleAdoptionAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleAdoptionRow>());
        _repo.CountUniqueUsersAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0);
        _repo.GetFeatureCountsAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleFeatureCountRow>());

        var handler = new GetModuleAdoption.Handler(_repo, _clock);

        // Act
        var result = await handler.Handle(new GetModuleAdoption.Query(null, null, null), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Modules.Should().BeEmpty();
        result.Value.OverallAdoptionScore.Should().Be(0m);
    }

    [Fact]
    public async Task GetModuleAdoption_WithData_ShouldCalculateAdoptionAndDepthScore()
    {
        // Arrange — two modules with different usage
        _repo.GetModuleAdoptionAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleAdoptionRow>
            {
                new(ProductModule.ChangeIntelligence, 300, 25),
                new(ProductModule.ContractStudio, 100, 10),
            });
        _repo.CountUniqueUsersAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(50);
        _repo.GetFeatureCountsAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleFeatureCountRow>
            {
                new(ProductModule.ChangeIntelligence, "blast-radius", 120),
                new(ProductModule.ChangeIntelligence, "timeline", 80),
                new(ProductModule.ContractStudio, "editor", 60),
            });

        var handler = new GetModuleAdoption.Handler(_repo, _clock);

        // Act
        var result = await handler.Handle(new GetModuleAdoption.Query(null, null, null), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Modules.Should().HaveCount(2);
        result.Value.OverallAdoptionScore.Should().BeGreaterThan(0m);

        // Ordered by TotalActions descending — ChangeIntelligence first
        result.Value.Modules[0].Module.Should().Be(ProductModule.ChangeIntelligence);
        result.Value.Modules[0].AdoptionPercent.Should().Be(50); // 25/50 * 100 = 50
        result.Value.Modules[0].TopFeatures.Should().Contain("blast-radius");

        // Depth score: ChangeIntelligence = 300/25 = 12 actions/user (max)
        // ContractStudio = 100/10 = 10 actions/user → depthScore = (10/12)*100 = 83.3
        result.Value.Modules[0].DepthScore.Should().Be(100m);
        result.Value.Modules[1].DepthScore.Should().BeApproximately(83.3m, 0.1m);

        result.Value.MostAdopted.Should().Be(ProductModule.ChangeIntelligence);
        result.Value.LeastAdopted.Should().Be(ProductModule.ContractStudio);
    }

    [Fact]
    public async Task GetModuleAdoption_WithPersonaFilter_ShouldPassPersonaToRepository()
    {
        // Arrange
        _repo.GetModuleAdoptionAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleAdoptionRow>
            {
                new(ProductModule.Governance, 40, 3),
            });
        _repo.CountUniqueUsersAsync(
            Arg.Any<string?>(), Arg.Any<ProductModule?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(3);
        _repo.GetFeatureCountsAsync(
            Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ModuleFeatureCountRow>());

        var handler = new GetModuleAdoption.Handler(_repo, _clock);

        // Act
        var result = await handler.Handle(
            new GetModuleAdoption.Query(Persona: "Executive", TeamId: null, Range: "last_7d"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Modules.Should().HaveCount(1);
        result.Value.PeriodLabel.Should().Be("last_7d");

        await _repo.Received(1).GetModuleAdoptionAsync(
            "Executive", Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }
}
