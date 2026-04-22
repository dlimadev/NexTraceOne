using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetContractLineageReport;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave AB.2 — GetContractLineageReport.
/// Cobre linhagem de versões, RetentionDays, StabilityScore, StabilityBand,
/// identificação de retenção extrema, filtro por ContractId e validador.
/// </summary>
public sealed class WaveAbContractLineageReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 11, 1, 0, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-ab2";
    private const string ContractId = "order-api";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static GetContractLineageReport.Handler CreateHandler(
        IContractVersionHistoryReader reader)
        => new(reader, CreateClock());

    private static ContractVersionEntry MakeEntry(
        string contractId,
        string contractName,
        string version,
        int breakingChanges,
        DateTimeOffset publishedAt,
        DateTimeOffset? deprecatedAt,
        string lifecycleState = "Published",
        string? protocol = "REST") =>
        new(contractId, contractName, version, lifecycleState,
            "alice@example.com", "approver@example.com",
            publishedAt, deprecatedAt, breakingChanges, 0, protocol);

    // ── 1. Tenant sem contratos devolve relatório vazio ───────────────────

    [Fact]
    public async Task Handler_ReturnsEmptyReport_WhenNoContracts()
    {
        var reader = Substitute.For<IContractVersionHistoryReader>();
        reader.ListContractIdsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>([]));

        var result = await CreateHandler(reader).Handle(
            new GetContractLineageReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Contracts.Should().BeEmpty();
        result.Value.TotalContractsAnalyzed.Should().Be(0);
    }

    // ── 2. Contrato único com 3 versões devolve LineageNodes correctos ────

    [Fact]
    public async Task Handler_SingleContract_ThreeVersions_ReturnsCorrectLineageNodes()
    {
        var base_ = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var versions = new List<ContractVersionEntry>
        {
            MakeEntry(ContractId, "Order API", "v1.0", 0, base_, base_.AddDays(30), "Deprecated"),
            MakeEntry(ContractId, "Order API", "v1.1", 1, base_.AddDays(30), base_.AddDays(90), "Deprecated"),
            MakeEntry(ContractId, "Order API", "v2.0", 2, base_.AddDays(90), null),
        };

        var reader = Substitute.For<IContractVersionHistoryReader>();
        reader.ListContractIdsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>([ContractId]));
        reader.ListByContractAsync(TenantId, ContractId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ContractVersionEntry>>(versions));

        var result = await CreateHandler(reader).Handle(
            new GetContractLineageReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var contract = result.Value.Contracts.Single();
        contract.TotalVersions.Should().Be(3);
        contract.Versions.Should().HaveCount(3);
    }

    // ── 3. RetentionDays calculado correctamente para activa vs deprecada ─

    [Fact]
    public async Task Handler_RetentionDays_ComputedCorrectly_ForActiveVsDeprecated()
    {
        var base_ = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var versions = new List<ContractVersionEntry>
        {
            // Deprecada: 30 dias de retenção
            MakeEntry(ContractId, "Order API", "v1.0", 0, base_, base_.AddDays(30), "Deprecated"),
            // Activa: FixedNow - base_.AddDays(30) = 305 dias
            MakeEntry(ContractId, "Order API", "v2.0", 0, base_.AddDays(30), null),
        };

        var reader = Substitute.For<IContractVersionHistoryReader>();
        reader.ListContractIdsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>([ContractId]));
        reader.ListByContractAsync(TenantId, ContractId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ContractVersionEntry>>(versions));

        var result = await CreateHandler(reader).Handle(
            new GetContractLineageReport.Query(TenantId), CancellationToken.None);

        var nodes = result.Value.Contracts.Single().Versions;
        nodes.Single(n => n.Version == "v1.0").RetentionDays.Should().Be(30);
        // v2.0 activa: FixedNow (2025-11-01) - base_.AddDays(30) (2025-01-31) = 274 dias
        nodes.Single(n => n.Version == "v2.0").RetentionDays.Should().BeGreaterThan(100);
    }

    // ── 4. StabilityScore é 1.0 sem breaking changes ─────────────────────

    [Fact]
    public async Task Handler_StabilityScore_IsOne_ForNoBreakingChanges()
    {
        var base_ = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var versions = new List<ContractVersionEntry>
        {
            MakeEntry(ContractId, "Order API", "v1.0", 0, base_, base_.AddDays(30), "Deprecated"),
            MakeEntry(ContractId, "Order API", "v1.1", 0, base_.AddDays(30), null),
        };

        var reader = Substitute.For<IContractVersionHistoryReader>();
        reader.ListContractIdsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>([ContractId]));
        reader.ListByContractAsync(TenantId, ContractId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ContractVersionEntry>>(versions));

        var result = await CreateHandler(reader).Handle(
            new GetContractLineageReport.Query(TenantId), CancellationToken.None);

        result.Value.Contracts.Single().StabilityScore.Should().Be(1.0);
    }

    // ── 5. StabilityScore < 1.0 quando há breaking changes ───────────────

    [Fact]
    public async Task Handler_StabilityScore_IsLessThanOne_WhenBreakingChangesExist()
    {
        var base_ = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var versions = new List<ContractVersionEntry>
        {
            MakeEntry(ContractId, "Order API", "v1.0", 0, base_, base_.AddDays(30), "Deprecated"),
            MakeEntry(ContractId, "Order API", "v2.0", 3, base_.AddDays(30), null),
        };

        var reader = Substitute.For<IContractVersionHistoryReader>();
        reader.ListContractIdsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>([ContractId]));
        reader.ListByContractAsync(TenantId, ContractId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ContractVersionEntry>>(versions));

        var result = await CreateHandler(reader).Handle(
            new GetContractLineageReport.Query(TenantId), CancellationToken.None);

        result.Value.Contracts.Single().StabilityScore.Should().BeLessThan(1.0);
    }

    // ── 6. StabilityBand Stable quando score ≥ 0.9 ──────────────────────

    [Fact]
    public async Task Handler_StabilityBand_IsStable_WhenScoreAtLeast09()
    {
        var base_ = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        // 1 breaking change em 10 transições → score = 1 - 1/10 = 0.9
        var versions = Enumerable.Range(0, 11)
            .Select(i => MakeEntry(ContractId, "Order API", $"v{i}.0",
                i == 5 ? 1 : 0, base_.AddDays(i * 10), i < 10 ? base_.AddDays((i + 1) * 10) : null,
                i < 10 ? "Deprecated" : "Published"))
            .ToList();

        var reader = Substitute.For<IContractVersionHistoryReader>();
        reader.ListContractIdsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>([ContractId]));
        reader.ListByContractAsync(TenantId, ContractId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ContractVersionEntry>>(versions));

        var result = await CreateHandler(reader).Handle(
            new GetContractLineageReport.Query(TenantId), CancellationToken.None);

        result.Value.Contracts.Single().StabilityBand
            .Should().Be(GetContractLineageReport.LineageStabilityBand.Stable);
    }

    // ── 7. StabilityBand Moderate quando score entre 0.7 e 0.9 ──────────

    [Fact]
    public async Task Handler_StabilityBand_IsModerate_WhenScoreBetween07And09()
    {
        var base_ = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        // 2 breaking changes em 5 transições → score = 1 - 2/5 = 0.6 → Volatile
        // Para Moderate precisamos de score entre 0.7 e 0.9: 1 breaking em 4 transições = 0.75
        var versions = new List<ContractVersionEntry>
        {
            MakeEntry(ContractId, "API", "v1", 0, base_, base_.AddDays(10), "Deprecated"),
            MakeEntry(ContractId, "API", "v2", 0, base_.AddDays(10), base_.AddDays(20), "Deprecated"),
            MakeEntry(ContractId, "API", "v3", 0, base_.AddDays(20), base_.AddDays(30), "Deprecated"),
            MakeEntry(ContractId, "API", "v4", 1, base_.AddDays(30), base_.AddDays(40), "Deprecated"),
            MakeEntry(ContractId, "API", "v5", 0, base_.AddDays(40), null),
        };

        var reader = Substitute.For<IContractVersionHistoryReader>();
        reader.ListContractIdsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>([ContractId]));
        reader.ListByContractAsync(TenantId, ContractId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ContractVersionEntry>>(versions));

        var result = await CreateHandler(reader).Handle(
            new GetContractLineageReport.Query(TenantId), CancellationToken.None);

        result.Value.Contracts.Single().StabilityBand
            .Should().Be(GetContractLineageReport.LineageStabilityBand.Moderate);
    }

    // ── 8. StabilityBand Volatile quando score entre 0.5 e 0.7 ──────────

    [Fact]
    public async Task Handler_StabilityBand_IsVolatile_WhenScoreBetween05And07()
    {
        var base_ = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        // 2 breaking changes em 4 transições = 1 - 0.5 = 0.5 → Volatile (edge: ≥0.5)
        // Para garantir Volatile (mas não Moderate): 1 - 2/5 = 0.6
        var versions = new List<ContractVersionEntry>
        {
            MakeEntry(ContractId, "API", "v1", 0, base_, base_.AddDays(10), "Deprecated"),
            MakeEntry(ContractId, "API", "v2", 1, base_.AddDays(10), base_.AddDays(20), "Deprecated"),
            MakeEntry(ContractId, "API", "v3", 0, base_.AddDays(20), base_.AddDays(30), "Deprecated"),
            MakeEntry(ContractId, "API", "v4", 1, base_.AddDays(30), base_.AddDays(40), "Deprecated"),
            MakeEntry(ContractId, "API", "v5", 0, base_.AddDays(40), null),
        };

        var reader = Substitute.For<IContractVersionHistoryReader>();
        reader.ListContractIdsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>([ContractId]));
        reader.ListByContractAsync(TenantId, ContractId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ContractVersionEntry>>(versions));

        var result = await CreateHandler(reader).Handle(
            new GetContractLineageReport.Query(TenantId), CancellationToken.None);

        result.Value.Contracts.Single().StabilityBand
            .Should().Be(GetContractLineageReport.LineageStabilityBand.Volatile);
    }

    // ── 9. StabilityBand HighlyVolatile quando score < 0.5 ───────────────

    [Fact]
    public async Task Handler_StabilityBand_IsHighlyVolatile_WhenScoreBelow05()
    {
        var base_ = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        // 3 breaking changes em 4 transições = 1 - 3/4 = 0.25 < 0.5
        var versions = new List<ContractVersionEntry>
        {
            MakeEntry(ContractId, "API", "v1", 0, base_, base_.AddDays(10), "Deprecated"),
            MakeEntry(ContractId, "API", "v2", 2, base_.AddDays(10), base_.AddDays(20), "Deprecated"),
            MakeEntry(ContractId, "API", "v3", 1, base_.AddDays(20), base_.AddDays(30), "Deprecated"),
            MakeEntry(ContractId, "API", "v4", 1, base_.AddDays(30), null),
        };

        var reader = Substitute.For<IContractVersionHistoryReader>();
        reader.ListContractIdsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>([ContractId]));
        reader.ListByContractAsync(TenantId, ContractId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ContractVersionEntry>>(versions));

        var result = await CreateHandler(reader).Handle(
            new GetContractLineageReport.Query(TenantId), CancellationToken.None);

        result.Value.Contracts.Single().StabilityBand
            .Should().Be(GetContractLineageReport.LineageStabilityBand.HighlyVolatile);
    }

    // ── 10. LongestRetentionVersion é correctamente identificada ─────────

    [Fact]
    public async Task Handler_LongestRetentionVersion_IsCorrectlyIdentified()
    {
        var base_ = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var versions = new List<ContractVersionEntry>
        {
            MakeEntry(ContractId, "API", "v1.0", 0, base_, base_.AddDays(100), "Deprecated"),  // 100 dias
            MakeEntry(ContractId, "API", "v2.0", 0, base_.AddDays(100), base_.AddDays(130), "Deprecated"), // 30 dias
            MakeEntry(ContractId, "API", "v3.0", 0, base_.AddDays(130), null), // activa
        };

        var reader = Substitute.For<IContractVersionHistoryReader>();
        reader.ListContractIdsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>([ContractId]));
        reader.ListByContractAsync(TenantId, ContractId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ContractVersionEntry>>(versions));

        var result = await CreateHandler(reader).Handle(
            new GetContractLineageReport.Query(TenantId), CancellationToken.None);

        var contract = result.Value.Contracts.Single();
        // v3 activa: FixedNow - base_.AddDays(130) = 305 - 130 = 175 dias
        contract.LongestRetentionVersion.Should().Be("v3.0");
    }

    // ── 11. ShortestRetentionVersion é correctamente identificada ─────────

    [Fact]
    public async Task Handler_ShortestRetentionVersion_IsCorrectlyIdentified()
    {
        var base_ = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var versions = new List<ContractVersionEntry>
        {
            MakeEntry(ContractId, "API", "v1.0", 0, base_, base_.AddDays(100), "Deprecated"),  // 100 dias
            MakeEntry(ContractId, "API", "v2.0", 0, base_.AddDays(100), base_.AddDays(115), "Deprecated"), // 15 dias
            MakeEntry(ContractId, "API", "v3.0", 0, base_.AddDays(115), null),
        };

        var reader = Substitute.For<IContractVersionHistoryReader>();
        reader.ListContractIdsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>([ContractId]));
        reader.ListByContractAsync(TenantId, ContractId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ContractVersionEntry>>(versions));

        var result = await CreateHandler(reader).Handle(
            new GetContractLineageReport.Query(TenantId), CancellationToken.None);

        result.Value.Contracts.Single().ShortestRetentionVersion.Should().Be("v2.0");
        result.Value.Contracts.Single().ShortestRetentionDays.Should().Be(15);
    }

    // ── 12. Filtro por ContractId retorna apenas o contrato especificado ──

    [Fact]
    public async Task Handler_ContractIdFilter_ReturnsOnlySpecifiedContract()
    {
        var base_ = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var versions = new List<ContractVersionEntry>
        {
            MakeEntry(ContractId, "Order API", "v1.0", 0, base_, null),
        };

        var reader = Substitute.For<IContractVersionHistoryReader>();
        reader.ListByContractAsync(TenantId, ContractId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ContractVersionEntry>>(versions));

        var result = await CreateHandler(reader).Handle(
            new GetContractLineageReport.Query(TenantId, ContractId: ContractId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Contracts.Should().HaveCount(1);
        result.Value.Contracts.Single().ContractId.Should().Be(ContractId);
    }

    // ── 13. GlobalStabilityScore é a média dos scores dos contratos ───────

    [Fact]
    public async Task Handler_GlobalStabilityScore_IsAverageOfContractScores()
    {
        var base_ = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Contrato 1: score = 1.0 (sem breaking changes, 1 transição)
        var v1 = new List<ContractVersionEntry>
        {
            MakeEntry("c1", "C1", "v1", 0, base_, base_.AddDays(30), "Deprecated"),
            MakeEntry("c1", "C1", "v2", 0, base_.AddDays(30), null),
        };

        // Contrato 2: score = 0.0 (1 breaking change, 1 transição → clamped to 0)
        var v2 = new List<ContractVersionEntry>
        {
            MakeEntry("c2", "C2", "v1", 0, base_, base_.AddDays(30), "Deprecated"),
            MakeEntry("c2", "C2", "v2", 2, base_.AddDays(30), null),
        };

        var reader = Substitute.For<IContractVersionHistoryReader>();
        reader.ListContractIdsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>(["c1", "c2"]));
        reader.ListByContractAsync(TenantId, "c1", Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ContractVersionEntry>>(v1));
        reader.ListByContractAsync(TenantId, "c2", Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ContractVersionEntry>>(v2));

        var result = await CreateHandler(reader).Handle(
            new GetContractLineageReport.Query(TenantId), CancellationToken.None);

        // GlobalStabilityScore = (1.0 + 0.0) / 2 = 0.5 (score de c2 é clamped a 0)
        result.Value.GlobalStabilityScore.Should().BeApproximately(0.5, 0.01);
    }

    // ── 14. TotalBreakingChanges agrega todos os contratos ────────────────

    [Fact]
    public async Task Handler_TotalBreakingChanges_AggregatesAcrossContracts()
    {
        var base_ = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var v1 = new List<ContractVersionEntry>
        {
            MakeEntry("c1", "C1", "v1", 2, base_, null),
        };
        var v2 = new List<ContractVersionEntry>
        {
            MakeEntry("c2", "C2", "v1", 3, base_, null),
        };

        var reader = Substitute.For<IContractVersionHistoryReader>();
        reader.ListContractIdsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>(["c1", "c2"]));
        reader.ListByContractAsync(TenantId, "c1", Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ContractVersionEntry>>(v1));
        reader.ListByContractAsync(TenantId, "c2", Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ContractVersionEntry>>(v2));

        var result = await CreateHandler(reader).Handle(
            new GetContractLineageReport.Query(TenantId), CancellationToken.None);

        result.Value.TotalBreakingChanges.Should().Be(5);
    }

    // ── 15. MaxVersionsPerContract limita versões retornadas ─────────────

    [Fact]
    public async Task Handler_MaxVersionsPerContract_LimitsVersionsReturned()
    {
        var base_ = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var versions = Enumerable.Range(1, 10)
            .Select(i => MakeEntry(ContractId, "API", $"v{i}.0", 0,
                base_.AddDays(i * 10), i < 10 ? base_.AddDays((i + 1) * 10) : null,
                i < 10 ? "Deprecated" : "Published"))
            .ToList();

        var reader = Substitute.For<IContractVersionHistoryReader>();
        reader.ListContractIdsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>([ContractId]));
        reader.ListByContractAsync(TenantId, ContractId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ContractVersionEntry>>(versions));

        var result = await CreateHandler(reader).Handle(
            new GetContractLineageReport.Query(TenantId, MaxVersionsPerContract: 3),
            CancellationToken.None);

        result.Value.Contracts.Single().Versions.Count.Should().Be(3);
    }

    // ── 16. Validador rejeita LookbackDays < 30 ──────────────────────────

    [Fact]
    public void Validator_RejectsLookbackDays_LessThan30()
    {
        var validator = new GetContractLineageReport.Validator();
        var query = new GetContractLineageReport.Query(TenantId, LookbackDays: 10);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(query.LookbackDays));
    }
}
