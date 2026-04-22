using System.Linq;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Services.Abstractions;
using NexTraceOne.Catalog.Application.Services.Features.GetUncatalogedServicesReport;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetContractDriftFromRealityReport;
using NexTraceOne.Catalog.Application.Services.Features.GetCatalogHealthMaintenanceReport;
using FluentValidation;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave AM — Auto-Cataloging &amp; Service Discovery Intelligence.
/// AM.1 GetUncatalogedServicesReport, AM.2 GetContractDriftFromRealityReport, AM.3 GetCatalogHealthMaintenanceReport.
/// </summary>
public sealed class WaveAmAutoCatalogingTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // AM.1 — GetUncatalogedServicesReport
    // ═══════════════════════════════════════════════════════════════════════

    private static GetUncatalogedServicesReport.Handler CreateAm1Handler(
        IUncatalogedServicesReader reader)
        => new(reader, CreateClock());

    private static IUncatalogedServicesReader ReaderWithSummary(
        int catalogedCount,
        params IUncatalogedServicesReader.UncatalogedServiceEntry[] entries)
    {
        var reader = Substitute.For<IUncatalogedServicesReader>();
        reader.GetSummaryAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new IUncatalogedServicesReader.UncatalogedServicesSummary(
                catalogedCount, entries)));
        return reader;
    }

    private static IUncatalogedServicesReader.UncatalogedServiceEntry MakeUncataloged(
        string name, int dailyCalls, string? owner = null)
        => new(name, FixedNow.AddDays(-10), FixedNow, dailyCalls,
            ["production"], owner);

    // ── AM.1 Test 1: Empty list → UncatalogedCount=0, risk=0, coverage=100 ─

    [Fact]
    public async Task Am1_EmptyList_Returns_ZeroRisk_FullCoverage()
    {
        var result = await CreateAm1Handler(ReaderWithSummary(10))
            .Handle(new GetUncatalogedServicesReport.Query("tenant-am1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UncatalogedCount.Should().Be(0);
        result.Value.ShadowServiceRisk.Should().Be(0m);
        result.Value.CatalogCoverageRate.Should().Be(100m);
    }

    // ── AM.1 Test 2: 1 uncataloged + 9 cataloged → risk=10%, coverage=90% ─

    [Fact]
    public async Task Am1_OneUncataloged_NineCataloged_Returns_TenPercentRisk()
    {
        var reader = ReaderWithSummary(9, MakeUncataloged("shadow-svc", 50));
        var result = await CreateAm1Handler(reader)
            .Handle(new GetUncatalogedServicesReport.Query("tenant-am1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UncatalogedCount.Should().Be(1);
        result.Value.ShadowServiceRisk.Should().Be(10m);
        result.Value.CatalogCoverageRate.Should().Be(90m);
    }

    // ── AM.1 Test 3: DailyCallCount >= 1000 → Critical ────────────────────

    [Fact]
    public async Task Am1_HighVolume_Returns_CriticalTier()
    {
        var reader = ReaderWithSummary(5, MakeUncataloged("high-vol", 1500));
        var result = await CreateAm1Handler(reader)
            .Handle(new GetUncatalogedServicesReport.Query("tenant-am1"), CancellationToken.None);

        result.Value.UncatalogedServices.Single().SuggestedTier
            .Should().Be(GetUncatalogedServicesReport.SuggestedServiceTier.Critical);
    }

    // ── AM.1 Test 4: DailyCallCount >= 100 and < 1000 → Standard ─────────

    [Fact]
    public async Task Am1_MediumVolume_Returns_StandardTier()
    {
        var reader = ReaderWithSummary(5, MakeUncataloged("med-vol", 500));
        var result = await CreateAm1Handler(reader)
            .Handle(new GetUncatalogedServicesReport.Query("tenant-am1"), CancellationToken.None);

        result.Value.UncatalogedServices.Single().SuggestedTier
            .Should().Be(GetUncatalogedServicesReport.SuggestedServiceTier.Standard);
    }

    // ── AM.1 Test 5: DailyCallCount < 100 → Internal ─────────────────────

    [Fact]
    public async Task Am1_LowVolume_Returns_InternalTier()
    {
        var reader = ReaderWithSummary(5, MakeUncataloged("low-vol", 20));
        var result = await CreateAm1Handler(reader)
            .Handle(new GetUncatalogedServicesReport.Query("tenant-am1"), CancellationToken.None);

        result.Value.UncatalogedServices.Single().SuggestedTier
            .Should().Be(GetUncatalogedServicesReport.SuggestedServiceTier.Internal);
    }

    // ── AM.1 Test 6: MinDailyCalls filter excludes low-volume ─────────────

    [Fact]
    public async Task Am1_MinDailyCalls_ExcludesLowVolume()
    {
        var reader = ReaderWithSummary(5,
            MakeUncataloged("included", 50),
            MakeUncataloged("excluded", 5));
        var result = await CreateAm1Handler(reader)
            .Handle(new GetUncatalogedServicesReport.Query("tenant-am1", MinDailyCalls: 10), CancellationToken.None);

        result.Value.UncatalogedCount.Should().Be(1);
        result.Value.UncatalogedServices.Single().ServiceName.Should().Be("included");
    }

    // ── AM.1 Test 7: QuickRegisterList has same count as UncatalogedServices

    [Fact]
    public async Task Am1_QuickRegisterList_MatchesUncatalogedCount()
    {
        var reader = ReaderWithSummary(5,
            MakeUncataloged("svc-a", 50),
            MakeUncataloged("svc-b", 200));
        var result = await CreateAm1Handler(reader)
            .Handle(new GetUncatalogedServicesReport.Query("tenant-am1"), CancellationToken.None);

        result.Value.QuickRegisterList.Count.Should().Be(result.Value.UncatalogedCount);
    }

    // ── AM.1 Test 8: QuickRegisterList entry has correct ServiceName and Tier

    [Fact]
    public async Task Am1_QuickRegisterList_HasCorrectServiceNameAndTier()
    {
        var reader = ReaderWithSummary(5, MakeUncataloged("critical-svc", 2000));
        var result = await CreateAm1Handler(reader)
            .Handle(new GetUncatalogedServicesReport.Query("tenant-am1"), CancellationToken.None);

        var entry = result.Value.QuickRegisterList.Single();
        entry.ServiceName.Should().Be("critical-svc");
        entry.SuggestedTier.Should().Be(GetUncatalogedServicesReport.SuggestedServiceTier.Critical);
    }

    // ── AM.1 Test 9: PossibleOwner propagated from reader entry ───────────

    [Fact]
    public async Task Am1_PossibleOwner_Propagated()
    {
        var reader = ReaderWithSummary(5, MakeUncataloged("owned-svc", 100, "team-alpha"));
        var result = await CreateAm1Handler(reader)
            .Handle(new GetUncatalogedServicesReport.Query("tenant-am1"), CancellationToken.None);

        result.Value.UncatalogedServices.Single().PossibleOwner.Should().Be("team-alpha");
        result.Value.QuickRegisterList.Single().SuggestedOwner.Should().Be("team-alpha");
    }

    // ── AM.1 Test 10: CatalogedCount=0, 1 uncataloged → risk=100% ─────────

    [Fact]
    public async Task Am1_NoCataloged_OneUncataloged_Returns_FullRisk()
    {
        var reader = ReaderWithSummary(0, MakeUncataloged("shadow", 50));
        var result = await CreateAm1Handler(reader)
            .Handle(new GetUncatalogedServicesReport.Query("tenant-am1"), CancellationToken.None);

        result.Value.ShadowServiceRisk.Should().Be(100m);
        result.Value.CatalogCoverageRate.Should().Be(0m);
    }

    // ── AM.1 Test 11: Report period uses clock.UtcNow ─────────────────────

    [Fact]
    public async Task Am1_Report_PeriodEnd_MatchesClock()
    {
        var result = await CreateAm1Handler(ReaderWithSummary(5))
            .Handle(new GetUncatalogedServicesReport.Query("tenant-am1", LookbackDays: 7), CancellationToken.None);

        result.Value.PeriodEnd.Should().Be(FixedNow);
        result.Value.PeriodStart.Should().Be(FixedNow.AddDays(-7));
    }

    // ── AM.1 Test 12: Validator empty TenantId → invalid ─────────────────

    [Fact]
    public async Task Am1_Validator_EmptyTenantId_IsInvalid()
    {
        var validator = new GetUncatalogedServicesReport.Validator();
        var result = await validator.ValidateAsync(new GetUncatalogedServicesReport.Query(""));
        result.IsValid.Should().BeFalse();
    }

    // ── AM.1 Test 13: Validator LookbackDays=0 → invalid ─────────────────

    [Fact]
    public async Task Am1_Validator_LookbackDaysZero_IsInvalid()
    {
        var validator = new GetUncatalogedServicesReport.Validator();
        var result = await validator.ValidateAsync(new GetUncatalogedServicesReport.Query("tenant", LookbackDays: 0));
        result.IsValid.Should().BeFalse();
    }

    // ── AM.1 Test 14: Validator LookbackDays=91 → invalid ────────────────

    [Fact]
    public async Task Am1_Validator_LookbackDays91_IsInvalid()
    {
        var validator = new GetUncatalogedServicesReport.Validator();
        var result = await validator.ValidateAsync(new GetUncatalogedServicesReport.Query("tenant", LookbackDays: 91));
        result.IsValid.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // AM.2 — GetContractDriftFromRealityReport
    // ═══════════════════════════════════════════════════════════════════════

    private static GetContractDriftFromRealityReport.Handler CreateAm2Handler(
        IContractDriftReader reader)
        => new(reader, CreateClock());

    private static IContractDriftReader DriftReaderWith(
        params IContractDriftReader.ContractRuntimeObservation[] obs)
    {
        var reader = Substitute.For<IContractDriftReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<IContractDriftReader.ContractRuntimeObservation>>(obs));
        return reader;
    }

    private static IContractDriftReader EmptyDriftReader() => DriftReaderWith();

    private static IContractDriftReader.ContractRuntimeObservation MakeObs(
        string contractId,
        string contractName,
        string serviceName,
        string[] docOps,
        string[] observedOps,
        string[] undocCalls,
        IContractDriftReader.UnusedOperation[]? unusedOps = null,
        string[]? paramMismatches = null)
        => new(contractId, contractName, serviceName,
            docOps, observedOps, undocCalls,
            unusedOps ?? [],
            paramMismatches ?? []);

    // ── AM.2 Test 1: Empty observations → 0 contracts, score=100 ─────────

    [Fact]
    public async Task Am2_EmptyObservations_Returns_ZeroContracts_FullScore()
    {
        var result = await CreateAm2Handler(EmptyDriftReader())
            .Handle(new GetContractDriftFromRealityReport.Query("tenant-am2"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalContractsAnalyzed.Should().Be(0);
        result.Value.TenantContractRealityScore.Should().Be(100m);
    }

    // ── AM.2 Test 2: 0 undoc calls, 0 unused ops → Aligned ───────────────

    [Fact]
    public async Task Am2_ZeroUndocumented_ZeroUnused_Returns_Aligned()
    {
        var obs = MakeObs("c1", "Contract1", "svc-a",
            ["GET /items", "POST /items"], ["GET /items", "POST /items"], []);
        var result = await CreateAm2Handler(DriftReaderWith(obs))
            .Handle(new GetContractDriftFromRealityReport.Query("tenant-am2"), CancellationToken.None);

        result.Value.ByContract.Single().DriftTier
            .Should().Be(GetContractDriftFromRealityReport.RealityDriftTier.Aligned);
    }

    // ── AM.2 Test 3: undoc rate ~5% → MinorDrift ─────────────────────────

    [Fact]
    public async Task Am2_FivePercentUndocRate_Returns_MinorDrift()
    {
        // 19 documented + 1 undocumented = 1/20 = 5%
        var docOps = Enumerable.Range(1, 19).Select(i => $"GET /op{i}").ToArray();
        var obs = MakeObs("c1", "Contract1", "svc-a",
            docOps, docOps, ["GET /ghost"]);
        var result = await CreateAm2Handler(DriftReaderWith(obs))
            .Handle(new GetContractDriftFromRealityReport.Query("tenant-am2"), CancellationToken.None);

        result.Value.ByContract.Single().DriftTier
            .Should().Be(GetContractDriftFromRealityReport.RealityDriftTier.MinorDrift);
    }

    // ── AM.2 Test 4: undoc rate 20% → SignificantDrift ───────────────────

    [Fact]
    public async Task Am2_TwentyPercentUndocRate_Returns_SignificantDrift()
    {
        // 4 documented + 1 undocumented = 1/5 = 20%
        var docOps = new[] { "GET /a", "GET /b", "GET /c", "GET /d" };
        var obs = MakeObs("c1", "Contract1", "svc-a",
            docOps, docOps, ["GET /ghost"]);
        var result = await CreateAm2Handler(DriftReaderWith(obs))
            .Handle(new GetContractDriftFromRealityReport.Query("tenant-am2"), CancellationToken.None);

        result.Value.ByContract.Single().DriftTier
            .Should().Be(GetContractDriftFromRealityReport.RealityDriftTier.SignificantDrift);
    }

    // ── AM.2 Test 5: undoc rate 35% → Misaligned ─────────────────────────

    [Fact]
    public async Task Am2_ThirtyFivePercentUndocRate_Returns_Misaligned()
    {
        // 3 documented + 2 undocumented = 2/5 = 40%
        var docOps = new[] { "GET /a", "GET /b", "GET /c" };
        var obs = MakeObs("c1", "Contract1", "svc-a",
            docOps, docOps, ["GET /ghost1", "GET /ghost2"]);
        var result = await CreateAm2Handler(DriftReaderWith(obs))
            .Handle(new GetContractDriftFromRealityReport.Query("tenant-am2"), CancellationToken.None);

        result.Value.ByContract.Single().DriftTier
            .Should().Be(GetContractDriftFromRealityReport.RealityDriftTier.Misaligned);
    }

    // ── AM.2 Test 6: 0 undoc calls but 15% unused ops → MinorDrift ───────

    [Fact]
    public async Task Am2_ZeroUndoc_FifteenPercentUnused_Returns_MinorDrift()
    {
        // 20 documented, 0 undocumented, 3 unused = 15%
        var docOps = Enumerable.Range(1, 20).Select(i => $"GET /op{i}").ToArray();
        var unusedOps = new[]
        {
            new IContractDriftReader.UnusedOperation("GET /op18", 35),
            new IContractDriftReader.UnusedOperation("GET /op19", 40),
            new IContractDriftReader.UnusedOperation("GET /op20", 32),
        };
        var obs = MakeObs("c1", "Contract1", "svc-a",
            docOps, docOps, [], unusedOps);
        var result = await CreateAm2Handler(DriftReaderWith(obs))
            .Handle(new GetContractDriftFromRealityReport.Query("tenant-am2"), CancellationToken.None);

        result.Value.ByContract.Single().DriftTier
            .Should().Be(GetContractDriftFromRealityReport.RealityDriftTier.MinorDrift);
    }

    // ── AM.2 Test 7: UndocumentedCallRate calculation correct ────────────

    [Fact]
    public async Task Am2_UndocRate_CalculatedCorrectly()
    {
        // 9 documented + 1 undocumented = 1/10 = 10%
        var docOps = new[] { "GET /a", "GET /b", "GET /c", "GET /d", "GET /e",
                             "GET /f", "GET /g", "GET /h", "GET /i" };
        var obs = MakeObs("c1", "Contract1", "svc-a",
            docOps, docOps, ["GET /ghost"]);
        var result = await CreateAm2Handler(DriftReaderWith(obs))
            .Handle(new GetContractDriftFromRealityReport.Query("tenant-am2"), CancellationToken.None);

        result.Value.ByContract.Single().UndocumentedCallRate.Should().Be(10m);
    }

    // ── AM.2 Test 8: UnusedOpsRate calculation correct ────────────────────

    [Fact]
    public async Task Am2_UnusedOpsRate_CalculatedCorrectly()
    {
        // 4 documented, 1 unused = 25%
        var docOps = new[] { "GET /a", "GET /b", "GET /c", "GET /d" };
        var unused = new[] { new IContractDriftReader.UnusedOperation("GET /d", 31) };
        var obs = MakeObs("c1", "Contract1", "svc-a", docOps, docOps, [], unused);
        var result = await CreateAm2Handler(DriftReaderWith(obs))
            .Handle(new GetContractDriftFromRealityReport.Query("tenant-am2"), CancellationToken.None);

        result.Value.ByContract.Single().UnusedOpsRate.Should().Be(25m);
    }

    // ── AM.2 Test 9: TenantScore = (Aligned + MinorDrift) / Total * 100 ──

    [Fact]
    public async Task Am2_TenantRealityScore_UsesAlignedAndMinor()
    {
        // 2 aligned + 1 misaligned = score 66.67
        var aligned = MakeObs("c1", "C1", "svc-a",
            ["GET /a"], ["GET /a"], []);
        var aligned2 = MakeObs("c2", "C2", "svc-b",
            ["GET /b"], ["GET /b"], []);
        // 3 doc + 2 undoc = 2/5 = 40% → Misaligned
        var misaligned = MakeObs("c3", "C3", "svc-c",
            ["GET /x", "GET /y", "GET /z"], ["GET /x"], ["GET /ghost1", "GET /ghost2"]);

        var result = await CreateAm2Handler(DriftReaderWith(aligned, aligned2, misaligned))
            .Handle(new GetContractDriftFromRealityReport.Query("tenant-am2"), CancellationToken.None);

        result.Value.AlignedCount.Should().Be(2);
        result.Value.MisalignedCount.Should().Be(1);
        result.Value.TenantContractRealityScore.Should().BeApproximately(66.67m, 0.1m);
    }

    // ── AM.2 Test 10: TopDriftingContracts capped at 10 ──────────────────

    [Fact]
    public async Task Am2_TopDriftingContracts_CappedAtTen()
    {
        var observations = Enumerable.Range(1, 15)
            .Select(i => MakeObs($"c{i}", $"Contract{i}", $"svc-{i}",
                ["GET /a"], ["GET /a"], ["GET /ghost"]))
            .ToArray();

        var result = await CreateAm2Handler(DriftReaderWith(observations))
            .Handle(new GetContractDriftFromRealityReport.Query("tenant-am2"), CancellationToken.None);

        result.Value.TopDriftingContracts.Count.Should().BeLessThanOrEqualTo(10);
    }

    // ── AM.2 Test 11: AutoDocumentationHints generated for UndocumentedCalls

    [Fact]
    public async Task Am2_AutoDocumentationHints_GeneratedForUndocCalls()
    {
        var obs = MakeObs("c1", "Contract1", "svc-a",
            ["GET /a"], ["GET /a"], ["GET /undoc1", "GET /undoc2"]);
        var result = await CreateAm2Handler(DriftReaderWith(obs))
            .Handle(new GetContractDriftFromRealityReport.Query("tenant-am2"), CancellationToken.None);

        result.Value.AutoDocumentationHints.Count.Should().Be(2);
    }

    // ── AM.2 Test 12: AutoDocHints contain "paths:\n" ─────────────────────

    [Fact]
    public async Task Am2_AutoDocHints_ContainPathsSnippet()
    {
        var obs = MakeObs("c1", "Contract1", "svc-a",
            ["GET /a"], ["GET /a"], ["GET /ghost"]);
        var result = await CreateAm2Handler(DriftReaderWith(obs))
            .Handle(new GetContractDriftFromRealityReport.Query("tenant-am2"), CancellationToken.None);

        result.Value.AutoDocumentationHints.Single().SuggestedOpenApiSnippet
            .Should().Contain("paths:\n");
    }

    // ── AM.2 Test 13: Validator empty TenantId → invalid ─────────────────

    [Fact]
    public async Task Am2_Validator_EmptyTenantId_IsInvalid()
    {
        var validator = new GetContractDriftFromRealityReport.Validator();
        var result = await validator.ValidateAsync(new GetContractDriftFromRealityReport.Query(""));
        result.IsValid.Should().BeFalse();
    }

    // ── AM.2 Test 14: Validator LookbackDays=0 → invalid ─────────────────

    [Fact]
    public async Task Am2_Validator_LookbackDaysZero_IsInvalid()
    {
        var validator = new GetContractDriftFromRealityReport.Validator();
        var result = await validator.ValidateAsync(
            new GetContractDriftFromRealityReport.Query("tenant", LookbackDays: 0));
        result.IsValid.Should().BeFalse();
    }

    // ── AM.2 Test 15: Validator UnusedOpsStagnationDays=0 → invalid ───────

    [Fact]
    public async Task Am2_Validator_UnusedOpsStagnationDaysZero_IsInvalid()
    {
        var validator = new GetContractDriftFromRealityReport.Validator();
        var result = await validator.ValidateAsync(
            new GetContractDriftFromRealityReport.Query("tenant", UnusedOpsStagnationDays: 0));
        result.IsValid.Should().BeFalse();
    }

    // ── AM.2 Test 16: AutoDocHints contain contract ID and call name ───────

    [Fact]
    public async Task Am2_AutoDocHints_ContainContractIdAndCallName()
    {
        var obs = MakeObs("contract-x", "ContractX", "svc-a",
            ["GET /a"], ["GET /a"], ["GET /new-endpoint"]);
        var result = await CreateAm2Handler(DriftReaderWith(obs))
            .Handle(new GetContractDriftFromRealityReport.Query("tenant-am2"), CancellationToken.None);

        var hint = result.Value.AutoDocumentationHints.Single();
        hint.ContractId.Should().Be("contract-x");
        hint.UndocumentedOperation.Should().Be("GET /new-endpoint");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // AM.3 — GetCatalogHealthMaintenanceReport
    // ═══════════════════════════════════════════════════════════════════════

    private static GetCatalogHealthMaintenanceReport.Handler CreateAm3Handler(
        ICatalogHealthMaintenanceReader reader)
        => new(reader, CreateClock());

    private static ICatalogHealthMaintenanceReader MaintenanceReaderWith(
        params ICatalogHealthMaintenanceReader.ServiceMaintenanceEntry[] entries)
    {
        var reader = Substitute.For<ICatalogHealthMaintenanceReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ICatalogHealthMaintenanceReader.ServiceMaintenanceEntry>>(entries));
        return reader;
    }

    private static ICatalogHealthMaintenanceReader.ServiceMaintenanceEntry MakeEntry(
        string id = "svc-1",
        string name = "Service1",
        string tier = "Standard",
        int descWords = 15,
        bool hasOwnership = true,
        bool hasContract = true,
        bool hasDependency = true,
        bool hasRunbook = true,
        int ownershipAgeDays = 30,
        int dependencyAgeDays = 20,
        int maintenanceAgeDays = 30)
        => new(id, name, tier,
            descWords,
            hasOwnership ? FixedNow.AddDays(-ownershipAgeDays) : (DateTimeOffset?)null,
            hasContract,
            hasDependency ? FixedNow.AddDays(-dependencyAgeDays) : (DateTimeOffset?)null,
            hasRunbook,
            FixedNow.AddDays(-maintenanceAgeDays));

    // ── AM.3 Test 1: Empty entries → score=100, Excellent, empty campaign ─

    [Fact]
    public async Task Am3_EmptyEntries_Returns_PerfectScore_Excellent()
    {
        var result = await CreateAm3Handler(MaintenanceReaderWith())
            .Handle(new GetCatalogHealthMaintenanceReport.Query("tenant-am3"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantCatalogHealthScore.Should().Be(100m);
        result.Value.OverallTier.Should().Be(GetCatalogHealthMaintenanceReport.CatalogQualityTier.Excellent);
        result.Value.CampaignList.Should().BeEmpty();
    }

    // ── AM.3 Test 2: All dims present → score=100, Excellent ─────────────

    [Fact]
    public async Task Am3_AllDimsPresent_Returns_PerfectScore()
    {
        var result = await CreateAm3Handler(MaintenanceReaderWith(MakeEntry()))
            .Handle(new GetCatalogHealthMaintenanceReport.Query("tenant-am3"), CancellationToken.None);

        result.Value.ByService.Single().CatalogQualityScore.Should().Be(100m);
        result.Value.ByService.Single().QualityTier
            .Should().Be(GetCatalogHealthMaintenanceReport.CatalogQualityTier.Excellent);
    }

    // ── AM.3 Test 3: Description missing → score=80 (0.25+0.25+0.15+0.15=80)

    [Fact]
    public async Task Am3_DescriptionMissing_Returns_ScoreEighty()
    {
        var entry = MakeEntry(descWords: 3);
        var result = await CreateAm3Handler(MaintenanceReaderWith(entry))
            .Handle(new GetCatalogHealthMaintenanceReport.Query("tenant-am3"), CancellationToken.None);

        result.Value.ByService.Single().CatalogQualityScore.Should().Be(80m);
    }

    // ── AM.3 Test 4: No approved contract → score=75 ─────────────────────

    [Fact]
    public async Task Am3_NoApprovedContract_Returns_ScoreSeventyFive()
    {
        var entry = MakeEntry(hasContract: false);
        var result = await CreateAm3Handler(MaintenanceReaderWith(entry))
            .Handle(new GetCatalogHealthMaintenanceReport.Query("tenant-am3"), CancellationToken.None);

        result.Value.ByService.Single().CatalogQualityScore.Should().Be(75m);
    }

    // ── AM.3 Test 5: Score >= 85 → Excellent ─────────────────────────────

    [Fact]
    public async Task Am3_ScoreAbove85_Returns_Excellent()
    {
        var result = await CreateAm3Handler(MaintenanceReaderWith(MakeEntry()))
            .Handle(new GetCatalogHealthMaintenanceReport.Query("tenant-am3"), CancellationToken.None);

        result.Value.ByService.Single().QualityTier
            .Should().Be(GetCatalogHealthMaintenanceReport.CatalogQualityTier.Excellent);
    }

    // ── AM.3 Test 6: Score >= 65 and < 85 → Good ─────────────────────────

    [Fact]
    public async Task Am3_ScoreBetween65And85_Returns_Good()
    {
        // desc missing (score=80) → Good
        var entry = MakeEntry(descWords: 3);
        var result = await CreateAm3Handler(MaintenanceReaderWith(entry))
            .Handle(new GetCatalogHealthMaintenanceReport.Query("tenant-am3"), CancellationToken.None);

        result.Value.ByService.Single().QualityTier
            .Should().Be(GetCatalogHealthMaintenanceReport.CatalogQualityTier.Good);
    }

    // ── AM.3 Test 7: Score >= 40 and < 65 → Fair ─────────────────────────

    [Fact]
    public async Task Am3_ScoreBetween40And65_Returns_Fair()
    {
        // desc + ownership missing = 0.25+0.15+0.15 = 55
        var entry = MakeEntry(descWords: 3, hasOwnership: false);
        var result = await CreateAm3Handler(MaintenanceReaderWith(entry))
            .Handle(new GetCatalogHealthMaintenanceReport.Query("tenant-am3"), CancellationToken.None);

        result.Value.ByService.Single().CatalogQualityScore.Should().Be(55m);
        result.Value.ByService.Single().QualityTier
            .Should().Be(GetCatalogHealthMaintenanceReport.CatalogQualityTier.Fair);
    }

    // ── AM.3 Test 8: Score < 40 → Poor ───────────────────────────────────

    [Fact]
    public async Task Am3_ScoreBelow40_Returns_Poor()
    {
        // only runbook present: 0.15 = 15
        var entry = MakeEntry(descWords: 3, hasOwnership: false, hasContract: false, hasDependency: false, hasRunbook: true);
        var result = await CreateAm3Handler(MaintenanceReaderWith(entry))
            .Handle(new GetCatalogHealthMaintenanceReport.Query("tenant-am3"), CancellationToken.None);

        result.Value.ByService.Single().QualityTier
            .Should().Be(GetCatalogHealthMaintenanceReport.CatalogQualityTier.Poor);
    }

    // ── AM.3 Test 9: Campaign list includes only Poor and Fair ────────────

    [Fact]
    public async Task Am3_CampaignList_IncludesOnlyPoorAndFair()
    {
        var excellent = MakeEntry("svc-1", "Excellent", "Standard");
        var poor = MakeEntry("svc-2", "Poor", "Standard", descWords: 1, hasOwnership: false, hasContract: false, hasDependency: false, hasRunbook: false);
        var result = await CreateAm3Handler(MaintenanceReaderWith(excellent, poor))
            .Handle(new GetCatalogHealthMaintenanceReport.Query("tenant-am3"), CancellationToken.None);

        result.Value.CampaignList.Should().HaveCount(1);
        result.Value.CampaignList.Single().ServiceName.Should().Be("Poor");
    }

    // ── AM.3 Test 10: Campaign list orders Critical services first ─────────

    [Fact]
    public async Task Am3_CampaignList_OrdersCriticalFirst()
    {
        var internalPoor = MakeEntry("svc-i", "InternalPoor", "Internal",
            descWords: 1, hasOwnership: false, hasContract: false, hasDependency: false, hasRunbook: false);
        var criticalPoor = MakeEntry("svc-c", "CriticalPoor", "Critical",
            descWords: 1, hasOwnership: false, hasContract: false, hasDependency: false, hasRunbook: false);
        var result = await CreateAm3Handler(MaintenanceReaderWith(internalPoor, criticalPoor))
            .Handle(new GetCatalogHealthMaintenanceReport.Query("tenant-am3"), CancellationToken.None);

        result.Value.CampaignList.First().ServiceTier.Should().Be("Critical");
    }

    // ── AM.3 Test 11: StaleEntryList includes services with null maintenance

    [Fact]
    public async Task Am3_StaleEntryList_IncludesNullMaintenanceServices()
    {
        // Create entry with null LastMaintenanceActivity by using a custom record
        var entry = new ICatalogHealthMaintenanceReader.ServiceMaintenanceEntry(
            "svc-stale", "StaleService", "Standard",
            15, FixedNow.AddDays(-10), true, FixedNow.AddDays(-10), true,
            null);
        var result = await CreateAm3Handler(MaintenanceReaderWith(entry))
            .Handle(new GetCatalogHealthMaintenanceReport.Query("tenant-am3"), CancellationToken.None);

        result.Value.StaleEntryList.Should().HaveCount(1);
        result.Value.StaleEntryList.Single().ServiceName.Should().Be("StaleService");
    }

    // ── AM.3 Test 12: TenantScore weighted: Critical=3x, Standard=2x, Int=1x

    [Fact]
    public async Task Am3_TenantScore_IsWeightedByServiceTier()
    {
        // Critical at 100, Internal at 0
        var critical = MakeEntry("svc-c", "Critical", "Critical");
        var internalPoor = MakeEntry("svc-i", "InternalPoor", "Internal",
            descWords: 1, hasOwnership: false, hasContract: false, hasDependency: false, hasRunbook: false);

        var result = await CreateAm3Handler(MaintenanceReaderWith(critical, internalPoor))
            .Handle(new GetCatalogHealthMaintenanceReport.Query("tenant-am3"), CancellationToken.None);

        // Weighted: (100*3 + 0*1) / (3+1) = 75
        result.Value.TenantCatalogHealthScore.Should().Be(75m);
    }

    // ── AM.3 Test 13: Critical poor drags score more than Internal poor ───

    [Fact]
    public async Task Am3_CriticalPoor_DragsScore_MoreThan_InternalPoor()
    {
        var excellent = MakeEntry("svc-e", "Excellent", "Standard");

        var withCriticalPoor = await CreateAm3Handler(MaintenanceReaderWith(
            excellent,
            MakeEntry("svc-c", "CriticalPoor", "Critical",
                descWords: 1, hasOwnership: false, hasContract: false, hasDependency: false, hasRunbook: false)))
            .Handle(new GetCatalogHealthMaintenanceReport.Query("tenant-am3"), CancellationToken.None);

        var withInternalPoor = await CreateAm3Handler(MaintenanceReaderWith(
            excellent,
            MakeEntry("svc-i", "InternalPoor", "Internal",
                descWords: 1, hasOwnership: false, hasContract: false, hasDependency: false, hasRunbook: false)))
            .Handle(new GetCatalogHealthMaintenanceReport.Query("tenant-am3"), CancellationToken.None);

        withCriticalPoor.Value.TenantCatalogHealthScore
            .Should().BeLessThan(withInternalPoor.Value.TenantCatalogHealthScore);
    }

    // ── AM.3 Test 14: Issue list populated correctly ──────────────────────

    [Fact]
    public async Task Am3_IssueList_PopulatedCorrectly()
    {
        var entry = MakeEntry(descWords: 3, hasContract: false);
        var result = await CreateAm3Handler(MaintenanceReaderWith(entry))
            .Handle(new GetCatalogHealthMaintenanceReport.Query("tenant-am3"), CancellationToken.None);

        var issues = result.Value.ByService.Single().Issues;
        issues.Should().Contain("Description too short or missing");
        issues.Should().Contain("No approved contract registered");
    }

    // ── AM.3 Test 15: Validator empty TenantId → invalid ─────────────────

    [Fact]
    public async Task Am3_Validator_EmptyTenantId_IsInvalid()
    {
        var validator = new GetCatalogHealthMaintenanceReport.Validator();
        var result = await validator.ValidateAsync(new GetCatalogHealthMaintenanceReport.Query(""));
        result.IsValid.Should().BeFalse();
    }

    // ── AM.3 Test 16: Validator MinDescriptionWords=0 → invalid ──────────

    [Fact]
    public async Task Am3_Validator_MinDescriptionWordsZero_IsInvalid()
    {
        var validator = new GetCatalogHealthMaintenanceReport.Validator();
        var result = await validator.ValidateAsync(
            new GetCatalogHealthMaintenanceReport.Query("tenant", MinDescriptionWords: 0));
        result.IsValid.Should().BeFalse();
    }
}
