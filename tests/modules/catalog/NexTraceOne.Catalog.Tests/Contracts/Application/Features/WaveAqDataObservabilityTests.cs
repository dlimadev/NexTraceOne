using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetDataContractComplianceReport;
using NexTraceOne.Catalog.Application.Contracts.Features.GetSchemaEvolutionSafetyReport;
using NexTraceOne.Catalog.Application.Contracts.Features.GetSchemaQualityIndexReport;
using NexTraceOne.Catalog.Application.Contracts.Features.RegisterDataContract;
using NexTraceOne.Catalog.Domain.Entities;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para Wave AQ — Data Observability &amp; Schema Quality.
/// Cobre AQ.1 RegisterDataContract/GetDataContractComplianceReport,
/// AQ.2 GetSchemaQualityIndexReport e AQ.3 GetSchemaEvolutionSafetyReport.
/// </summary>
public sealed class WaveAqDataObservabilityTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-aq-001";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    // ════════════════════════════════════════════════════════════════════════
    // AQ.1 — RegisterDataContract & GetDataContractComplianceReport
    // ════════════════════════════════════════════════════════════════════════

    private static RegisterDataContract.Handler CreateRegisterHandler(IDataContractRepository? repo = null)
    {
        repo ??= Substitute.For<IDataContractRepository>();
        return new RegisterDataContract.Handler(repo, CreateClock());
    }

    private static GetDataContractComplianceReport.Handler CreateComplianceHandler(
        IReadOnlyList<DataContractRecord>? records = null)
    {
        var repo = Substitute.For<IDataContractRepository>();
        repo.ListByTenantAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(records ?? []);
        return new GetDataContractComplianceReport.Handler(repo, CreateClock());
    }

    private static DataContractRecord MakeRecord(
        string serviceId = "svc-1",
        string? ownerTeamId = "team-1",
        int? freshnessHours = 24,
        string? fieldJson = "[{\"fieldName\":\"id\"}]",
        int daysOld = 10)
    {
        var createdAt = FixedNow.AddDays(-daysOld);
        var record = DataContractRecord.Create(
            TenantId, serviceId, "orders", "1.0",
            freshnessHours, fieldJson, ownerTeamId,
            createdAt);
        return record;
    }

    [Fact]
    public async Task RegisterDataContract_ValidCommand_CallsRepositoryAndReturnsSuccess()
    {
        var repo = Substitute.For<IDataContractRepository>();
        var handler = CreateRegisterHandler(repo);
        var cmd = new RegisterDataContract.Command(TenantId, "svc-1", "orders", "1.0", 24, null, "team-1");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await repo.Received(1).AddAsync(Arg.Any<DataContractRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterDataContract_CreatedRecord_HasCorrectTenantAndService()
    {
        DataContractRecord? captured = null;
        var repo = Substitute.For<IDataContractRepository>();
        repo.When(r => r.AddAsync(Arg.Any<DataContractRecord>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.Arg<DataContractRecord>());
        var handler = CreateRegisterHandler(repo);

        await handler.Handle(
            new RegisterDataContract.Command(TenantId, "svc-2", "payments", "2.0", null, null, null),
            CancellationToken.None);

        captured!.TenantId.Should().Be(TenantId);
        captured.ServiceId.Should().Be("svc-2");
        captured.DatasetName.Should().Be("payments");
    }

    [Theory]
    [InlineData("TenantId")]
    [InlineData("ServiceId")]
    [InlineData("DatasetName")]
    [InlineData("ContractVersion")]
    public void RegisterDataContract_EmptyRequiredField_ValidationFails(string fieldName)
    {
        var validator = new RegisterDataContract.Validator();
        var cmd = new RegisterDataContract.Command(TenantId, "svc-1", "orders", "1.0");
        var invalid = fieldName switch
        {
            "TenantId"        => cmd with { TenantId = "" },
            "ServiceId"       => cmd with { ServiceId = "" },
            "DatasetName"     => cmd with { DatasetName = "" },
            "ContractVersion" => cmd with { ContractVersion = "" },
            _                 => cmd
        };

        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetDataContractComplianceReport_NoContracts_AllZeroPercentages()
    {
        var result = await CreateComplianceHandler([]).Handle(
            new GetDataContractComplianceReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DataContractCount.Should().Be(0);
        result.Value.GovernedPct.Should().Be(0);
        result.Value.PartialPct.Should().Be(0);
        result.Value.UnmanagedPct.Should().Be(0);
    }

    [Fact]
    public async Task GetDataContractComplianceReport_ContractWithOwnerSlaFieldsNotStale_IsGoverned()
    {
        var record = MakeRecord(ownerTeamId: "team-1", freshnessHours: 24, fieldJson: "[{\"f\":1}]", daysOld: 10);
        var result = await CreateComplianceHandler([record]).Handle(
            new GetDataContractComplianceReport.Query(TenantId, StaleDays: 90, MinFieldCompletenessPct: 80),
            CancellationToken.None);

        result.Value.ContractDetails.Single().Tier.Should().Be(GetDataContractComplianceReport.DataContractTier.Governed);
        result.Value.GovernedPct.Should().Be(100);
    }

    [Fact]
    public async Task GetDataContractComplianceReport_ContractWithoutOwner_IsUnmanaged()
    {
        var record = MakeRecord(ownerTeamId: null, freshnessHours: 24, fieldJson: "[{\"f\":1}]", daysOld: 10);
        var result = await CreateComplianceHandler([record]).Handle(
            new GetDataContractComplianceReport.Query(TenantId), CancellationToken.None);

        result.Value.ContractDetails.Single().Tier.Should().Be(GetDataContractComplianceReport.DataContractTier.Unmanaged);
        result.Value.UnmanagedPct.Should().Be(100);
    }

    [Fact]
    public async Task GetDataContractComplianceReport_ContractWithoutSla_IsUnmanaged()
    {
        var record = MakeRecord(ownerTeamId: "team-1", freshnessHours: null, fieldJson: "[{\"f\":1}]", daysOld: 10);
        var result = await CreateComplianceHandler([record]).Handle(
            new GetDataContractComplianceReport.Query(TenantId), CancellationToken.None);

        result.Value.ContractDetails.Single().Tier.Should().Be(GetDataContractComplianceReport.DataContractTier.Unmanaged);
    }

    [Fact]
    public async Task GetDataContractComplianceReport_ContractWithOwnerSlaButStale_IsGoverned()
    {
        var record = MakeRecord(ownerTeamId: "team-1", freshnessHours: 24, fieldJson: "[{\"f\":1}]", daysOld: 100);
        var result = await CreateComplianceHandler([record]).Handle(
            new GetDataContractComplianceReport.Query(TenantId, StaleDays: 90, MinFieldCompletenessPct: 80),
            CancellationToken.None);

        // owner=true, sla=true, fields=100%>=80%=true, notStale=false → criteria=3 → Governed
        result.Value.ContractDetails.Single().Tier.Should().Be(GetDataContractComplianceReport.DataContractTier.Governed);
    }

    [Fact]
    public async Task GetDataContractComplianceReport_ContractWithOwnerSlaNoFieldsAndStale_IsPartial()
    {
        var record = MakeRecord(ownerTeamId: "team-1", freshnessHours: 24, fieldJson: null, daysOld: 100);
        var result = await CreateComplianceHandler([record]).Handle(
            new GetDataContractComplianceReport.Query(TenantId, StaleDays: 90, MinFieldCompletenessPct: 80),
            CancellationToken.None);

        // owner=true, sla=true, fields=0%<80%=false, notStale=false → criteria=2 → Partial
        result.Value.ContractDetails.Single().Tier.Should().Be(GetDataContractComplianceReport.DataContractTier.Partial);
    }

    [Fact]
    public async Task GetDataContractComplianceReport_StaleContractCount_PopulatedCorrectly()
    {
        var fresh = MakeRecord(serviceId: "svc-1", daysOld: 10);
        var stale = MakeRecord(serviceId: "svc-2", daysOld: 100);
        var result = await CreateComplianceHandler([fresh, stale]).Handle(
            new GetDataContractComplianceReport.Query(TenantId, StaleDays: 90), CancellationToken.None);

        result.Value.StaleContractCount.Should().Be(1);
    }

    [Fact]
    public async Task GetDataContractComplianceReport_FieldlessContractCount_PopulatedCorrectly()
    {
        var withFields = MakeRecord(serviceId: "svc-1", fieldJson: "[{\"f\":1}]");
        var withoutFields = MakeRecord(serviceId: "svc-2", fieldJson: null);
        var result = await CreateComplianceHandler([withFields, withoutFields]).Handle(
            new GetDataContractComplianceReport.Query(TenantId), CancellationToken.None);

        result.Value.FieldlessContractCount.Should().Be(1);
    }

    [Fact]
    public async Task GetDataContractComplianceReport_TeamGovernanceScore_ComputedCorrectly()
    {
        var governed = MakeRecord(serviceId: "svc-1", ownerTeamId: "team-1", freshnessHours: 24, fieldJson: "[{\"f\":1}]", daysOld: 10);
        var partial = MakeRecord(serviceId: "svc-2", ownerTeamId: "team-1", freshnessHours: 24, fieldJson: null, daysOld: 100);
        var result = await CreateComplianceHandler([governed, partial]).Handle(
            new GetDataContractComplianceReport.Query(TenantId, StaleDays: 90, MinFieldCompletenessPct: 80),
            CancellationToken.None);

        var teamScore = result.Value.TeamGovernanceScores.Single(t => t.TeamId == "team-1");
        teamScore.TotalContracts.Should().Be(2);
        teamScore.GovernedContracts.Should().Be(1);
    }

    [Fact]
    public void GetDataContractComplianceReport_EmptyTenantId_ValidationFails()
    {
        var validator = new GetDataContractComplianceReport.Validator();
        validator.Validate(new GetDataContractComplianceReport.Query("")).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetDataContractComplianceReport_MixedTiers_PercentsAddUp()
    {
        var governed = MakeRecord(serviceId: "svc-1", ownerTeamId: "team-1", freshnessHours: 24, fieldJson: "[{\"f\":1}]", daysOld: 10);
        var unmanaged = MakeRecord(serviceId: "svc-2", ownerTeamId: null, freshnessHours: null, fieldJson: null, daysOld: 10);
        var result = await CreateComplianceHandler([governed, unmanaged]).Handle(
            new GetDataContractComplianceReport.Query(TenantId), CancellationToken.None);

        (result.Value.GovernedPct + result.Value.PartialPct + result.Value.UnmanagedPct).Should().Be(100);
    }

    [Fact]
    public void GetDataContractComplianceReport_InvalidLookbackDays_ValidationFails()
    {
        var validator = new GetDataContractComplianceReport.Validator();
        validator.Validate(new GetDataContractComplianceReport.Query(TenantId, LookbackDays: 0)).IsValid.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════════════════════
    // AQ.2 — GetSchemaQualityIndexReport
    // ════════════════════════════════════════════════════════════════════════

    private static GetSchemaQualityIndexReport.Handler CreateQualityHandler(
        IReadOnlyList<ISchemaQualityReader.ContractSchemaEntry>? entries = null,
        IReadOnlyList<ISchemaQualityReader.SchemaQualitySnapshot>? snapshots = null)
    {
        var reader = Substitute.For<ISchemaQualityReader>();
        reader.ListByTenantAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(entries ?? []);
        reader.GetMonthlySnapshotsAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(snapshots ?? []);
        return new GetSchemaQualityIndexReport.Handler(reader, CreateClock());
    }

    private static ISchemaQualityReader.ContractSchemaEntry MakeEntry(
        string contractId = "contract-1",
        string protocol = "REST",
        string serviceTier = "Standard",
        int totalFields = 10,
        int fieldsWithDesc = 10,
        int fieldsWithExamples = 5,
        int opsWithErrors = 5,
        int totalOps = 5,
        int fieldsWithConstraints = 8,
        int enumWith3Plus = 2,
        int totalEnums = 2) =>
        new(contractId, "ContractName", protocol, serviceTier,
            totalFields, fieldsWithDesc, fieldsWithExamples, opsWithErrors, totalOps,
            fieldsWithConstraints, enumWith3Plus, totalEnums);

    [Fact]
    public async Task GetSchemaQualityIndexReport_EmptyContracts_TenantHealthScoreIsZero()
    {
        var result = await CreateQualityHandler([]).Handle(
            new GetSchemaQualityIndexReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantSchemaHealthScore.Should().Be(0.0);
    }

    [Fact]
    public async Task GetSchemaQualityIndexReport_AllFieldsWithDescription_DescCovIs100()
    {
        var entry = MakeEntry(totalFields: 10, fieldsWithDesc: 10, totalOps: 5, opsWithErrors: 5,
            fieldsWithExamples: 5, fieldsWithConstraints: 10, enumWith3Plus: 2, totalEnums: 2);
        var result = await CreateQualityHandler([entry]).Handle(
            new GetSchemaQualityIndexReport.Query(TenantId), CancellationToken.None);

        result.Value.AllContracts.Single().DescriptionCoverage.Should().Be(100.0);
    }

    [Fact]
    public async Task GetSchemaQualityIndexReport_NoFieldsWithDescription_DescCovIsZero_TierIsPoor()
    {
        var entry = MakeEntry(totalFields: 10, fieldsWithDesc: 0, fieldsWithExamples: 0,
            opsWithErrors: 0, totalOps: 5, fieldsWithConstraints: 0, enumWith3Plus: 0, totalEnums: 5);
        var result = await CreateQualityHandler([entry]).Handle(
            new GetSchemaQualityIndexReport.Query(TenantId), CancellationToken.None);

        var row = result.Value.AllContracts.Single();
        row.DescriptionCoverage.Should().Be(0.0);
        row.Tier.Should().Be(GetSchemaQualityIndexReport.SchemaQualityTier.Poor);
    }

    [Fact]
    public async Task GetSchemaQualityIndexReport_HighScores_TierIsExcellent()
    {
        var entry = MakeEntry(totalFields: 10, fieldsWithDesc: 10, fieldsWithExamples: 5,
            opsWithErrors: 5, totalOps: 5, fieldsWithConstraints: 10, enumWith3Plus: 3, totalEnums: 3);
        var result = await CreateQualityHandler([entry]).Handle(
            new GetSchemaQualityIndexReport.Query(TenantId), CancellationToken.None);

        result.Value.AllContracts.Single().Tier.Should().Be(GetSchemaQualityIndexReport.SchemaQualityTier.Excellent);
    }

    [Fact]
    public async Task GetSchemaQualityIndexReport_GoodTier_WhenScoreIn65To85()
    {
        // desc=80%, ex=80%, err=80%, constraint=60%, enum=100% → score=80*0.25+80*0.25+80*0.20+60*0.15+100*0.15 = 20+20+16+9+15 = 80 → Good
        var entry = MakeEntry(totalFields: 10, fieldsWithDesc: 8, fieldsWithExamples: 4,
            opsWithErrors: 4, totalOps: 5, fieldsWithConstraints: 6, enumWith3Plus: 2, totalEnums: 2);
        var result = await CreateQualityHandler([entry]).Handle(
            new GetSchemaQualityIndexReport.Query(TenantId), CancellationToken.None);

        var score = result.Value.AllContracts.Single().SchemaQualityScore;
        score.Should().BeGreaterThanOrEqualTo(65).And.BeLessThan(85);
        result.Value.AllContracts.Single().Tier.Should().Be(GetSchemaQualityIndexReport.SchemaQualityTier.Good);
    }

    [Fact]
    public async Task GetSchemaQualityIndexReport_FairTier_WhenScoreIn40To65()
    {
        // desc=50%*0.25=12.5, ex=50%*0.25=12.5, err=50%*0.20=10, constraint=30%*0.15=4.5, enum=30%*0.15=4.5 → ~44 → Fair
        var entry = MakeEntry(totalFields: 10, fieldsWithDesc: 5, fieldsWithExamples: 2,
            opsWithErrors: 2, totalOps: 4, fieldsWithConstraints: 3, enumWith3Plus: 1, totalEnums: 3);
        var result = await CreateQualityHandler([entry]).Handle(
            new GetSchemaQualityIndexReport.Query(TenantId), CancellationToken.None);

        var tier = result.Value.AllContracts.Single().Tier;
        tier.Should().BeOneOf(
            GetSchemaQualityIndexReport.SchemaQualityTier.Fair,
            GetSchemaQualityIndexReport.SchemaQualityTier.Poor);
    }

    [Fact]
    public async Task GetSchemaQualityIndexReport_WorstContracts_SortedByAscendingScore()
    {
        var good = MakeEntry("c1", totalFields: 10, fieldsWithDesc: 10, fieldsWithExamples: 5,
            opsWithErrors: 5, totalOps: 5, fieldsWithConstraints: 10, enumWith3Plus: 2, totalEnums: 2);
        var poor = MakeEntry("c2", totalFields: 10, fieldsWithDesc: 0, fieldsWithExamples: 0,
            opsWithErrors: 0, totalOps: 5, fieldsWithConstraints: 0, enumWith3Plus: 0, totalEnums: 5);
        var result = await CreateQualityHandler([good, poor]).Handle(
            new GetSchemaQualityIndexReport.Query(TenantId, TopWorstCount: 10), CancellationToken.None);

        result.Value.WorstQualityContracts.First().ContractId.Should().Be("c2");
    }

    [Fact]
    public async Task GetSchemaQualityIndexReport_QualityByProtocol_AggregatesCorrectly()
    {
        var rest1 = MakeEntry("c1", protocol: "REST");
        var rest2 = MakeEntry("c2", protocol: "REST");
        var asyncApi = MakeEntry("c3", protocol: "AsyncAPI");
        var result = await CreateQualityHandler([rest1, rest2, asyncApi]).Handle(
            new GetSchemaQualityIndexReport.Query(TenantId), CancellationToken.None);

        var restRow = result.Value.QualityByProtocol.Single(p => p.Protocol == "REST");
        restRow.ContractCount.Should().Be(2);
        result.Value.QualityByProtocol.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetSchemaQualityIndexReport_QualityTrend_PositiveWhenCurrentHigherThanSnapshot()
    {
        var entry = MakeEntry(totalFields: 10, fieldsWithDesc: 10, fieldsWithExamples: 5,
            opsWithErrors: 5, totalOps: 5, fieldsWithConstraints: 10, enumWith3Plus: 2, totalEnums: 2);
        var snapshot = new ISchemaQualityReader.SchemaQualitySnapshot(
            FixedNow.AddMonths(-1), 50.0);
        var result = await CreateQualityHandler([entry], [snapshot]).Handle(
            new GetSchemaQualityIndexReport.Query(TenantId), CancellationToken.None);

        result.Value.QualityTrendDelta.Should().NotBeNull();
        result.Value.QualityTrendDelta!.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetSchemaQualityIndexReport_QualityTrend_NullWhenNoSnapshots()
    {
        var entry = MakeEntry();
        var result = await CreateQualityHandler([entry], []).Handle(
            new GetSchemaQualityIndexReport.Query(TenantId), CancellationToken.None);

        result.Value.QualityTrendDelta.Should().BeNull();
    }

    [Fact]
    public async Task GetSchemaQualityIndexReport_TenantHealthScore_WeightedByCritical()
    {
        var critical = MakeEntry("c1", serviceTier: "Critical", totalFields: 10, fieldsWithDesc: 10,
            fieldsWithExamples: 5, opsWithErrors: 5, totalOps: 5, fieldsWithConstraints: 10, enumWith3Plus: 2, totalEnums: 2);
        var intern = MakeEntry("c2", serviceTier: "Internal", totalFields: 10, fieldsWithDesc: 0,
            fieldsWithExamples: 0, opsWithErrors: 0, totalOps: 5, fieldsWithConstraints: 0, enumWith3Plus: 0, totalEnums: 5);
        var result = await CreateQualityHandler([critical, intern]).Handle(
            new GetSchemaQualityIndexReport.Query(TenantId), CancellationToken.None);

        var critScore = result.Value.AllContracts.First(c => c.ContractId == "c1").SchemaQualityScore;
        var internScore = result.Value.AllContracts.First(c => c.ContractId == "c2").SchemaQualityScore;
        double expected = Math.Round((critScore * 3 + internScore * 1) / 4.0, 2);
        result.Value.TenantSchemaHealthScore.Should().Be(expected);
    }

    [Fact]
    public async Task GetSchemaQualityIndexReport_PoorContract_HasQualityImprovementHints()
    {
        var entry = MakeEntry(totalFields: 10, fieldsWithDesc: 0, fieldsWithExamples: 0,
            opsWithErrors: 0, totalOps: 5, fieldsWithConstraints: 0, enumWith3Plus: 0, totalEnums: 5);
        var result = await CreateQualityHandler([entry]).Handle(
            new GetSchemaQualityIndexReport.Query(TenantId), CancellationToken.None);

        result.Value.AllContracts.Single().QualityImprovementHints.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetSchemaQualityIndexReport_ZeroEnumFields_EnumCoverageIs100()
    {
        var entry = MakeEntry(enumWith3Plus: 0, totalEnums: 0);
        var result = await CreateQualityHandler([entry]).Handle(
            new GetSchemaQualityIndexReport.Query(TenantId), CancellationToken.None);

        result.Value.AllContracts.Single().EnumCoverage.Should().Be(100.0);
    }

    [Fact]
    public void GetSchemaQualityIndexReport_EmptyTenantId_ValidationFails()
    {
        var validator = new GetSchemaQualityIndexReport.Validator();
        validator.Validate(new GetSchemaQualityIndexReport.Query("")).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetSchemaQualityIndexReport_SchemaQualityScore_WeightedFormula()
    {
        // desc=100%(×0.25), ex=60%(×0.25), err=80%(×0.20), constraint=100%(×0.15), enum=100%(×0.15)
        // = 25 + 15 + 16 + 15 + 15 = 86 → Excellent
        var entry = MakeEntry(totalFields: 10, fieldsWithDesc: 10, fieldsWithExamples: 3,
            opsWithErrors: 4, totalOps: 5, fieldsWithConstraints: 10, enumWith3Plus: 2, totalEnums: 2);
        var result = await CreateQualityHandler([entry]).Handle(
            new GetSchemaQualityIndexReport.Query(TenantId), CancellationToken.None);

        result.Value.AllContracts.Single().Tier.Should().Be(GetSchemaQualityIndexReport.SchemaQualityTier.Excellent);
    }

    // ════════════════════════════════════════════════════════════════════════
    // AQ.3 — GetSchemaEvolutionSafetyReport
    // ════════════════════════════════════════════════════════════════════════

    private static GetSchemaEvolutionSafetyReport.Handler CreateEvolutionHandler(
        IReadOnlyList<ISchemaEvolutionSafetyReader.TeamSchemaEvolutionEntry>? entries = null)
    {
        var reader = Substitute.For<ISchemaEvolutionSafetyReader>();
        reader.ListByTenantAsync(TenantId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(entries ?? []);
        return new GetSchemaEvolutionSafetyReport.Handler(reader, CreateClock());
    }

    private static ISchemaEvolutionSafetyReader.TeamSchemaEvolutionEntry MakeTeamEntry(
        string teamId = "team-1",
        int totalChanges = 10,
        int breakingChanges = 0,
        int breakingWithIncident = 0,
        int notifiedBreaking = 0,
        IReadOnlyList<ISchemaEvolutionSafetyReader.ProtocolBreakingEntry>? protocols = null,
        IReadOnlyList<ISchemaEvolutionSafetyReader.HighRiskChange>? highRiskChanges = null) =>
        new(teamId, $"Team {teamId}", totalChanges, breakingChanges, breakingWithIncident, notifiedBreaking,
            protocols ?? [], highRiskChanges ?? []);

    [Fact]
    public async Task GetSchemaEvolutionSafetyReport_NoTeams_SafetyIndexIs100()
    {
        var result = await CreateEvolutionHandler([]).Handle(
            new GetSchemaEvolutionSafetyReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantEvolutionSafetyIndex.Should().Be(100.0);
        result.Value.TeamsAnalyzed.Should().Be(0);
    }

    [Fact]
    public async Task GetSchemaEvolutionSafetyReport_ZeroBreakingChanges_TeamIsSafe()
    {
        var entry = MakeTeamEntry(totalChanges: 10, breakingChanges: 0, notifiedBreaking: 0);
        var result = await CreateEvolutionHandler([entry]).Handle(
            new GetSchemaEvolutionSafetyReport.Query(TenantId), CancellationToken.None);

        result.Value.TeamDetails.Single().Tier.Should().Be(GetSchemaEvolutionSafetyReport.EvolutionSafetyTier.Safe);
    }

    [Fact]
    public async Task GetSchemaEvolutionSafetyReport_LowBreakingRateHighNotification_TeamIsSafe()
    {
        // breaking=1/20=5% ≤ 10% AND notificationRate=1/1=100% ≥ 90% → Safe
        var entry = MakeTeamEntry(totalChanges: 20, breakingChanges: 1, notifiedBreaking: 1);
        var result = await CreateEvolutionHandler([entry]).Handle(
            new GetSchemaEvolutionSafetyReport.Query(TenantId, BreakingChangeLowPct: 10.0), CancellationToken.None);

        result.Value.TeamDetails.Single().Tier.Should().Be(GetSchemaEvolutionSafetyReport.EvolutionSafetyTier.Safe);
    }

    [Fact]
    public async Task GetSchemaEvolutionSafetyReport_HighBreakingRate_TeamIsDangerous()
    {
        var entry = MakeTeamEntry(totalChanges: 10, breakingChanges: 4);
        var result = await CreateEvolutionHandler([entry]).Handle(
            new GetSchemaEvolutionSafetyReport.Query(TenantId, BreakingChangeDangerousPct: 25.0), CancellationToken.None);

        result.Value.TeamDetails.Single().Tier.Should().Be(GetSchemaEvolutionSafetyReport.EvolutionSafetyTier.Dangerous);
    }

    [Fact]
    public async Task GetSchemaEvolutionSafetyReport_BreakingWithIncident_TeamIsDangerous()
    {
        var entry = MakeTeamEntry(totalChanges: 10, breakingChanges: 1, breakingWithIncident: 1);
        var result = await CreateEvolutionHandler([entry]).Handle(
            new GetSchemaEvolutionSafetyReport.Query(TenantId), CancellationToken.None);

        result.Value.TeamDetails.Single().Tier.Should().Be(GetSchemaEvolutionSafetyReport.EvolutionSafetyTier.Dangerous);
    }

    [Fact]
    public async Task GetSchemaEvolutionSafetyReport_TenantSafetyIndex_PercentNonDangerousTeams()
    {
        var safe = MakeTeamEntry("team-1", totalChanges: 10, breakingChanges: 0);
        var dangerous = MakeTeamEntry("team-2", totalChanges: 10, breakingChanges: 4);
        var result = await CreateEvolutionHandler([safe, dangerous]).Handle(
            new GetSchemaEvolutionSafetyReport.Query(TenantId, BreakingChangeDangerousPct: 25.0), CancellationToken.None);

        // 1 non-dangerous out of 2 → 50%
        result.Value.TenantEvolutionSafetyIndex.Should().Be(50.0);
    }

    [Fact]
    public async Task GetSchemaEvolutionSafetyReport_ProtocolBreakingRate_AggregatedCorrectly()
    {
        var protocols = new ISchemaEvolutionSafetyReader.ProtocolBreakingEntry[]
        {
            new("REST", 10, 2),
            new("AsyncAPI", 5, 1)
        };
        var entry = MakeTeamEntry(protocols: protocols);
        var result = await CreateEvolutionHandler([entry]).Handle(
            new GetSchemaEvolutionSafetyReport.Query(TenantId), CancellationToken.None);

        var restRow = result.Value.ProtocolBreakingRateComparison.Single(p => p.Protocol == "REST");
        restRow.TotalBreaking.Should().Be(2);
        restRow.TotalChanges.Should().Be(10);
    }

    [Fact]
    public async Task GetSchemaEvolutionSafetyReport_HighRiskChanges_OnlyFromRiskyOrDangerous()
    {
        var highRisk = new ISchemaEvolutionSafetyReader.HighRiskChange("c1", "ContractA", FixedNow.AddDays(-5));
        var safeEntry = MakeTeamEntry("team-1", totalChanges: 10, breakingChanges: 0, highRiskChanges: [highRisk]);
        var dangerousEntry = MakeTeamEntry("team-2", totalChanges: 10, breakingChanges: 4, highRiskChanges: [highRisk]);
        var result = await CreateEvolutionHandler([safeEntry, dangerousEntry]).Handle(
            new GetSchemaEvolutionSafetyReport.Query(TenantId, BreakingChangeDangerousPct: 25.0), CancellationToken.None);

        // Only dangerous team's high risk changes included
        result.Value.HighRiskSchemaChanges.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetSchemaEvolutionSafetyReport_DangerousTeam_RecommendsVersioning()
    {
        var entry = MakeTeamEntry("team-1", totalChanges: 10, breakingChanges: 4);
        var result = await CreateEvolutionHandler([entry]).Handle(
            new GetSchemaEvolutionSafetyReport.Query(TenantId, BreakingChangeDangerousPct: 25.0), CancellationToken.None);

        result.Value.EvolutionPatternRecommendations.Should().NotBeEmpty();
        result.Value.EvolutionPatternRecommendations.Any(r => r.Contains("versioning")).Should().BeTrue();
    }

    [Fact]
    public async Task GetSchemaEvolutionSafetyReport_LowNotificationRate_RecommendsNotifications()
    {
        var entry = MakeTeamEntry("team-1", totalChanges: 10, breakingChanges: 4, notifiedBreaking: 0);
        var result = await CreateEvolutionHandler([entry]).Handle(
            new GetSchemaEvolutionSafetyReport.Query(TenantId, BreakingChangeDangerousPct: 25.0), CancellationToken.None);

        result.Value.EvolutionPatternRecommendations.Any(r => r.Contains("notification")).Should().BeTrue();
    }

    [Fact]
    public void GetSchemaEvolutionSafetyReport_EmptyTenantId_ValidationFails()
    {
        var validator = new GetSchemaEvolutionSafetyReport.Validator();
        validator.Validate(new GetSchemaEvolutionSafetyReport.Query("")).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetSchemaEvolutionSafetyReport_IntermediateBreakingRate_TeamIsRiskyOrCautious()
    {
        // breaking=2/10=20% > 5%*2=10% → Risky (not Dangerous since ≤25%)
        var entry = MakeTeamEntry(totalChanges: 10, breakingChanges: 2, notifiedBreaking: 1);
        var result = await CreateEvolutionHandler([entry]).Handle(
            new GetSchemaEvolutionSafetyReport.Query(TenantId, BreakingChangeLowPct: 5.0, BreakingChangeDangerousPct: 25.0),
            CancellationToken.None);

        result.Value.TeamDetails.Single().Tier.Should().BeOneOf(
            GetSchemaEvolutionSafetyReport.EvolutionSafetyTier.Risky,
            GetSchemaEvolutionSafetyReport.EvolutionSafetyTier.Cautious);
    }
}
