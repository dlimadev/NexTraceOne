using System.Linq;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetIncidentKnowledgeBaseReport;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes unitários para Wave AB.3 — GetIncidentKnowledgeBaseReport.
/// Cobre ResolutionConfidence, RunbookEffectivenessScore, KnowledgeGap, StaleRunbook,
/// TopGaps, KnowledgeMaturityScore, MaturityLevel e casos de borda.
/// </summary>
public sealed class GetIncidentKnowledgeBaseReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 11, 1, 0, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-ab3";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static GetIncidentKnowledgeBaseReport.Handler CreateHandler(
        IIncidentKnowledgeReader reader)
        => new(reader, CreateClock());

    private static IncidentTypeKnowledgeEntry MakeEntry(
        string type,
        int total,
        int withRunbook,
        int effectiveResolutions,
        bool hasRunbook,
        bool isStale = false,
        bool trendIncreasing = false) =>
        new(type, total, withRunbook, 5.0, effectiveResolutions,
            hasRunbook, isStale, FixedNow.AddDays(-10), trendIncreasing);

    // ── 1. Tenant vazio devolve relatório vazio ───────────────────────────

    [Fact]
    public async Task Handler_ReturnsEmptyReport_ForEmptyTenant()
    {
        var reader = Substitute.For<IIncidentKnowledgeReader>();
        reader.ListByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<IncidentTypeKnowledgeEntry>>([]));

        var result = await CreateHandler(reader).Handle(
            new GetIncidentKnowledgeBaseReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalIncidentTypes.Should().Be(0);
        result.Value.MaturityLevel.Should().Be(
            GetIncidentKnowledgeBaseReport.KnowledgeMaturityLevel.Nascent);
    }

    // ── 2. ResolutionConfidence é 0 quando não há runbook ────────────────

    [Fact]
    public async Task Handler_ResolutionConfidence_IsZero_WhenNoRunbookExists()
    {
        var reader = Substitute.For<IIncidentKnowledgeReader>();
        reader.ListByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<IncidentTypeKnowledgeEntry>>(
            [
                MakeEntry("TypeA", 10, 0, 0, false),
            ]));

        var result = await CreateHandler(reader).Handle(
            new GetIncidentKnowledgeBaseReport.Query(TenantId), CancellationToken.None);

        result.Value.IncidentTypes.Single().ResolutionConfidence.Should().Be(0.0);
    }

    // ── 3. ResolutionConfidence é 1.0 quando todas as ocorrências têm runbook

    [Fact]
    public async Task Handler_ResolutionConfidence_IsOne_WhenAllOccurrencesHaveRunbook()
    {
        var reader = Substitute.For<IIncidentKnowledgeReader>();
        reader.ListByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<IncidentTypeKnowledgeEntry>>(
            [
                MakeEntry("TypeA", 10, 10, 8, true),
            ]));

        var result = await CreateHandler(reader).Handle(
            new GetIncidentKnowledgeBaseReport.Query(TenantId), CancellationToken.None);

        result.Value.IncidentTypes.Single().ResolutionConfidence.Should().Be(1.0);
    }

    // ── 4. RunbookEffectivenessScore é calculado correctamente ────────────

    [Fact]
    public async Task Handler_RunbookEffectivenessScore_IsComputedCorrectly()
    {
        var reader = Substitute.For<IIncidentKnowledgeReader>();
        // 6 com runbook, 4 efectivos → score = 4/6 ≈ 0.6667
        reader.ListByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<IncidentTypeKnowledgeEntry>>(
            [
                MakeEntry("TypeA", 10, 6, 4, true),
            ]));

        var result = await CreateHandler(reader).Handle(
            new GetIncidentKnowledgeBaseReport.Query(TenantId), CancellationToken.None);

        result.Value.IncidentTypes.Single().RunbookEffectivenessScore
            .Should().BeApproximately(0.6667, 0.001);
    }

    // ── 5. KnowledgeGap é verdade quando ocorrências > 3 e sem runbook ───

    [Fact]
    public async Task Handler_KnowledgeGap_IsTrue_WhenOccurrencesAboveThreshold_AndNoRunbook()
    {
        var reader = Substitute.For<IIncidentKnowledgeReader>();
        reader.ListByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<IncidentTypeKnowledgeEntry>>(
            [
                MakeEntry("TypeA", 5, 0, 0, false),
            ]));

        var result = await CreateHandler(reader).Handle(
            new GetIncidentKnowledgeBaseReport.Query(TenantId), CancellationToken.None);

        result.Value.IncidentTypes.Single().KnowledgeGap.Should().BeTrue();
    }

    // ── 6. KnowledgeGap é falso quando ocorrências ≤ 3 ─────────────────

    [Fact]
    public async Task Handler_KnowledgeGap_IsFalse_WhenOccurrencesAtMostThree()
    {
        var reader = Substitute.For<IIncidentKnowledgeReader>();
        reader.ListByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<IncidentTypeKnowledgeEntry>>(
            [
                MakeEntry("TypeA", 3, 0, 0, false),
            ]));

        var result = await CreateHandler(reader).Handle(
            new GetIncidentKnowledgeBaseReport.Query(TenantId), CancellationToken.None);

        result.Value.IncidentTypes.Single().KnowledgeGap.Should().BeFalse();
    }

    // ── 7. KnowledgeGap é falso quando existe runbook, independente do count

    [Fact]
    public async Task Handler_KnowledgeGap_IsFalse_WhenRunbookExists()
    {
        var reader = Substitute.For<IIncidentKnowledgeReader>();
        reader.ListByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<IncidentTypeKnowledgeEntry>>(
            [
                MakeEntry("TypeA", 10, 5, 4, hasRunbook: true),
            ]));

        var result = await CreateHandler(reader).Handle(
            new GetIncidentKnowledgeBaseReport.Query(TenantId), CancellationToken.None);

        result.Value.IncidentTypes.Single().KnowledgeGap.Should().BeFalse();
    }

    // ── 8. StaleRunbook é propagado da entrada ────────────────────────────

    [Fact]
    public async Task Handler_StaleRunbook_IsSetFromEntry()
    {
        var reader = Substitute.For<IIncidentKnowledgeReader>();
        reader.ListByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<IncidentTypeKnowledgeEntry>>(
            [
                MakeEntry("TypeA", 5, 5, 4, true, isStale: true),
            ]));

        var result = await CreateHandler(reader).Handle(
            new GetIncidentKnowledgeBaseReport.Query(TenantId), CancellationToken.None);

        result.Value.IncidentTypes.Single().StaleRunbook.Should().BeTrue();
    }

    // ── 9. TopGaps contém apenas tipos com KnowledgeGap=true ─────────────

    [Fact]
    public async Task Handler_TopGaps_ContainsOnlyKnowledgeGapTypes()
    {
        var reader = Substitute.For<IIncidentKnowledgeReader>();
        reader.ListByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<IncidentTypeKnowledgeEntry>>(
            [
                MakeEntry("TypeGap", 10, 0, 0, false),    // gap
                MakeEntry("TypeOk", 10, 10, 8, true),     // sem gap
            ]));

        var result = await CreateHandler(reader).Handle(
            new GetIncidentKnowledgeBaseReport.Query(TenantId), CancellationToken.None);

        result.Value.TopGaps.Should().AllSatisfy(s => s.KnowledgeGap.Should().BeTrue());
        result.Value.TopGaps.Should().NotContain(s => s.IncidentType == "TypeOk");
    }

    // ── 10. TopGaps é limitado ao TopIncidents configurado ───────────────

    [Fact]
    public async Task Handler_TopGaps_IsLimitedToTopIncidentsCount()
    {
        var entries = Enumerable.Range(1, 10)
            .Select(i => MakeEntry($"TypeGap{i}", i + 4, 0, 0, false))
            .ToList();

        var reader = Substitute.For<IIncidentKnowledgeReader>();
        reader.ListByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<IncidentTypeKnowledgeEntry>>(entries));

        var result = await CreateHandler(reader).Handle(
            new GetIncidentKnowledgeBaseReport.Query(TenantId, TopIncidents: 3),
            CancellationToken.None);

        result.Value.TopGaps.Count.Should().BeLessThanOrEqualTo(3);
    }

    // ── 11. KnowledgeMaturityScore é a média ponderada dos tipos ─────────

    [Fact]
    public async Task Handler_KnowledgeMaturityScore_IsWeightedAverage()
    {
        var reader = Substitute.For<IIncidentKnowledgeReader>();
        // TypeA: ResolutionConfidence=1.0, RunbookEffectiveness=1.0 → weighted = 1.0
        // TypeB: ResolutionConfidence=0.0, RunbookEffectiveness=0.0 → weighted = 0.0
        // avg = 0.5
        reader.ListByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<IncidentTypeKnowledgeEntry>>(
            [
                MakeEntry("TypeA", 10, 10, 10, true),
                MakeEntry("TypeB", 10, 0, 0, false),
            ]));

        var result = await CreateHandler(reader).Handle(
            new GetIncidentKnowledgeBaseReport.Query(TenantId), CancellationToken.None);

        result.Value.KnowledgeMaturityScore.Should().BeApproximately(0.5, 0.01);
    }

    // ── 12. MaturityLevel Exemplary quando score ≥ 0.85 ─────────────────

    [Fact]
    public async Task Handler_MaturityLevel_IsExemplary_WhenScoreAtLeast085()
    {
        var reader = Substitute.For<IIncidentKnowledgeReader>();
        reader.ListByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<IncidentTypeKnowledgeEntry>>(
            [
                MakeEntry("TypeA", 10, 10, 9, true),   // RC=1.0, RE=0.9 → 0.95
            ]));

        var result = await CreateHandler(reader).Handle(
            new GetIncidentKnowledgeBaseReport.Query(TenantId), CancellationToken.None);

        result.Value.MaturityLevel
            .Should().Be(GetIncidentKnowledgeBaseReport.KnowledgeMaturityLevel.Exemplary);
    }

    // ── 13. MaturityLevel Mature quando score ≥ 0.70 e < 0.85 ──────────

    [Fact]
    public async Task Handler_MaturityLevel_IsMature_WhenScoreBetween070And085()
    {
        var reader = Substitute.For<IIncidentKnowledgeReader>();
        // RC = 8/10 = 0.8, RE = 6/8 = 0.75 → weighted = 0.8*0.5 + 0.75*0.5 = 0.775
        reader.ListByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<IncidentTypeKnowledgeEntry>>(
            [
                MakeEntry("TypeA", 10, 8, 6, true),
            ]));

        var result = await CreateHandler(reader).Handle(
            new GetIncidentKnowledgeBaseReport.Query(TenantId), CancellationToken.None);

        result.Value.MaturityLevel
            .Should().Be(GetIncidentKnowledgeBaseReport.KnowledgeMaturityLevel.Mature);
    }

    // ── 14. MaturityLevel Nascent quando score < 0.50 ────────────────────

    [Fact]
    public async Task Handler_MaturityLevel_IsNascent_WhenScoreBelow050()
    {
        var reader = Substitute.For<IIncidentKnowledgeReader>();
        // RC = 2/10 = 0.2, RE = 1/2 = 0.5 → weighted = 0.2*0.5 + 0.5*0.5 = 0.35
        reader.ListByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<IncidentTypeKnowledgeEntry>>(
            [
                MakeEntry("TypeA", 10, 2, 1, true),
            ]));

        var result = await CreateHandler(reader).Handle(
            new GetIncidentKnowledgeBaseReport.Query(TenantId), CancellationToken.None);

        result.Value.MaturityLevel
            .Should().Be(GetIncidentKnowledgeBaseReport.KnowledgeMaturityLevel.Nascent);
    }
}
