using NSubstitute;
using NexTraceOne.Catalog.Application.Services.Abstractions;
using NexTraceOne.Catalog.Application.Services.Features.GetOnboardingHealthReport;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave AC.1 — GetOnboardingHealthReport.
/// Cobre scoring por dimensão, OnboardingTier, TopLowestScores, TeamAverages e TenantOnboardingScore.
/// </summary>
public sealed class WaveAcOnboardingHealthReportTests
{
    private const string TenantId = "tenant-ac1";

    private static GetOnboardingHealthReport.Handler CreateHandler(IOnboardingHealthReader reader)
        => new(reader);

    private static IOnboardingHealthReader EmptyReader()
    {
        var reader = Substitute.For<IOnboardingHealthReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceOnboardingEntry>>([]));
        return reader;
    }

    private static IOnboardingHealthReader ReaderWith(params ServiceOnboardingEntry[] entries)
    {
        var reader = Substitute.For<IOnboardingHealthReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceOnboardingEntry>>(entries));
        return reader;
    }

    private static ServiceOnboardingEntry MakeEntry(
        string name,
        string? team = "team-a",
        string tier = "Standard",
        bool ownership = false,
        bool contract = false,
        bool runbook = false,
        bool slo = false,
        bool profiling = false)
        => new(name, team, tier, ownership, contract, runbook, slo, profiling);

    // ── 1. Tenant sem serviços devolve relatório vazio ────────────────────

    [Fact]
    public async Task Handler_ReturnsEmptyReport_ForEmptyTenant()
    {
        var result = await CreateHandler(EmptyReader())
            .Handle(new GetOnboardingHealthReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Services.Should().BeEmpty();
        result.Value.TotalServicesAnalyzed.Should().Be(0);
        result.Value.TenantOnboardingScore.Should().Be(0.0);
    }

    // ── 2. Todas as 5 dimensões presentes → score 100 ─────────────────────

    [Fact]
    public async Task Handler_Score100_WhenAllDimensionsPresent()
    {
        var entry = MakeEntry("svc", ownership: true, contract: true, runbook: true, slo: true, profiling: true);

        var result = await CreateHandler(ReaderWith(entry))
            .Handle(new GetOnboardingHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.Services[0].OnboardingScore.Should().Be(100);
    }

    // ── 3. Sem Ownership → score 80 (100 - 20) ───────────────────────────

    [Fact]
    public async Task Handler_Score80_WhenOwnershipMissing()
    {
        var entry = MakeEntry("svc", ownership: false, contract: true, runbook: true, slo: true, profiling: true);

        var result = await CreateHandler(ReaderWith(entry))
            .Handle(new GetOnboardingHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.Services[0].OnboardingScore.Should().Be(80);
    }

    // ── 4. Sem Contracts → score 75 (100 - 25) ───────────────────────────

    [Fact]
    public async Task Handler_Score75_WhenContractsMissing()
    {
        var entry = MakeEntry("svc", ownership: true, contract: false, runbook: true, slo: true, profiling: true);

        var result = await CreateHandler(ReaderWith(entry))
            .Handle(new GetOnboardingHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.Services[0].OnboardingScore.Should().Be(75);
    }

    // ── 5. Apenas Ownership → score 20 ───────────────────────────────────

    [Fact]
    public async Task Handler_Score20_WhenOnlyOwnershipPresent()
    {
        var entry = MakeEntry("svc", ownership: true, contract: false, runbook: false, slo: false, profiling: false);

        var result = await CreateHandler(ReaderWith(entry))
            .Handle(new GetOnboardingHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.Services[0].OnboardingScore.Should().Be(20);
    }

    // ── 6. Tier Complete quando score ≥ 90 ───────────────────────────────

    [Fact]
    public async Task Handler_TierComplete_WhenScoreAbove90()
    {
        var entry = MakeEntry("svc", ownership: true, contract: true, runbook: true, slo: true, profiling: true);

        var result = await CreateHandler(ReaderWith(entry))
            .Handle(new GetOnboardingHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.Services[0].Tier.Should().Be(GetOnboardingHealthReport.OnboardingTier.Complete);
    }

    // ── 7. Tier Advanced quando score ≥ 70 e < 90 ───────────────────────

    [Fact]
    public async Task Handler_TierAdvanced_WhenScoreBetween70And89()
    {
        // score = 20+25+20+15 = 80 (sem SLO)
        var entry = MakeEntry("svc", ownership: true, contract: true, runbook: true, slo: false, profiling: true);

        var result = await CreateHandler(ReaderWith(entry))
            .Handle(new GetOnboardingHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.Services[0].OnboardingScore.Should().Be(80);
        result.Value.Services[0].Tier.Should().Be(GetOnboardingHealthReport.OnboardingTier.Advanced);
    }

    // ── 8. Tier Basic quando score ≥ 40 e < 70 ───────────────────────────

    [Fact]
    public async Task Handler_TierBasic_WhenScoreBetween40And69()
    {
        // score = 20+25 = 45
        var entry = MakeEntry("svc", ownership: true, contract: true, runbook: false, slo: false, profiling: false);

        var result = await CreateHandler(ReaderWith(entry))
            .Handle(new GetOnboardingHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.Services[0].OnboardingScore.Should().Be(45);
        result.Value.Services[0].Tier.Should().Be(GetOnboardingHealthReport.OnboardingTier.Basic);
    }

    // ── 9. Tier Minimal quando score < 40 ────────────────────────────────

    [Fact]
    public async Task Handler_TierMinimal_WhenScoreBelow40()
    {
        var entry = MakeEntry("svc", ownership: false, contract: false, runbook: false, slo: false, profiling: false);

        var result = await CreateHandler(ReaderWith(entry))
            .Handle(new GetOnboardingHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.Services[0].Tier.Should().Be(GetOnboardingHealthReport.OnboardingTier.Minimal);
    }

    // ── 10. TopLowestScores ordenados por score ascendente ────────────────

    [Fact]
    public async Task Handler_TopLowestScores_OrderedByAscendingScore()
    {
        var entries = new[]
        {
            MakeEntry("svc-a", ownership: true, contract: true, runbook: true, slo: true, profiling: true),  // 100
            MakeEntry("svc-b", ownership: true, contract: false, runbook: false, slo: false, profiling: false), // 20
            MakeEntry("svc-c", ownership: true, contract: true, runbook: false, slo: false, profiling: false), // 45
        };

        var result = await CreateHandler(ReaderWith(entries))
            .Handle(new GetOnboardingHealthReport.Query(TenantId), CancellationToken.None);

        var lowest = result.Value.TopLowestScores;
        lowest[0].ServiceName.Should().Be("svc-b");
        lowest[1].ServiceName.Should().Be("svc-c");
        lowest[2].ServiceName.Should().Be("svc-a");
    }

    // ── 11. TopLowestCount limita o número de resultados ─────────────────

    [Fact]
    public async Task Handler_TopLowestScores_LimitedByTopLowestCount()
    {
        var entries = Enumerable.Range(1, 5)
            .Select(i => MakeEntry($"svc-{i}"))
            .ToArray();

        var result = await CreateHandler(ReaderWith(entries))
            .Handle(new GetOnboardingHealthReport.Query(TenantId, TopLowestCount: 2), CancellationToken.None);

        result.Value.TopLowestScores.Count.Should().Be(2);
    }

    // ── 12. TeamAverages agrega corretamente por equipa ───────────────────

    [Fact]
    public async Task Handler_TeamAverages_GroupsCorrectly()
    {
        var entries = new[]
        {
            MakeEntry("svc-a", team: "team-x", ownership: true, contract: true, runbook: true, slo: true, profiling: true),  // 100
            MakeEntry("svc-b", team: "team-x", ownership: true, contract: false, runbook: false, slo: false, profiling: false), // 20
            MakeEntry("svc-c", team: "team-y", ownership: true, contract: true, runbook: true, slo: false, profiling: false), // 65
        };

        var result = await CreateHandler(ReaderWith(entries))
            .Handle(new GetOnboardingHealthReport.Query(TenantId), CancellationToken.None);

        var teamX = result.Value.TeamAverages.Single(t => t.TeamName == "team-x");
        teamX.AvgScore.Should().Be(60.0);
        teamX.ServiceCount.Should().Be(2);

        var teamY = result.Value.TeamAverages.Single(t => t.TeamName == "team-y");
        teamY.AvgScore.Should().Be(65.0);
        teamY.ServiceCount.Should().Be(1);
    }

    // ── 13. TenantOnboardingScore é a média de todos os scores ───────────

    [Fact]
    public async Task Handler_TenantOnboardingScore_IsMeanOfAllServiceScores()
    {
        var entries = new[]
        {
            MakeEntry("svc-a", ownership: true, contract: true, runbook: true, slo: true, profiling: true),  // 100
            MakeEntry("svc-b", ownership: false, contract: false, runbook: false, slo: false, profiling: false), // 0
        };

        var result = await CreateHandler(ReaderWith(entries))
            .Handle(new GetOnboardingHealthReport.Query(TenantId), CancellationToken.None);

        result.Value.TenantOnboardingScore.Should().Be(50.0);
    }
}
