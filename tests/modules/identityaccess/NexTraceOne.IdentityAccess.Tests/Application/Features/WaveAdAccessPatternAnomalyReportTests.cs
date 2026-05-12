using FluentAssertions;
using NSubstitute;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.GetAccessPatternAnomalyReport;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave AD.3 — GetAccessPatternAnomalyReport.
/// Cobre detecção de sinais por tipo, RiskScore, AnomalyDensityFlag,
/// distribuição por sinal, HighDensityUsers e Validator.
/// </summary>
public sealed class WaveAdAccessPatternAnomalyReportTests
{
    private const string TenantId = "tenant-ad3";

    private static GetAccessPatternAnomalyReport.Handler CreateHandler(
        IReadOnlyList<UserAccessEntry> entries)
    {
        var reader = Substitute.For<IAccessPatternReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(entries));
        return new GetAccessPatternAnomalyReport.Handler(reader);
    }

    private static UserAccessEntry NormalUser(string id, string? team = "team-a")
        => new(id, id, team,
            TotalRequests: 10,
            OffHoursRequests: 0,
            SensitiveResourceAccesses: 0,
            UnusualResourceAccesses: 0,
            BulkExportCount: 0,
            AvgDailyRequests: 1.0,
            MaxDailyRequests: 2.0);

    private static GetAccessPatternAnomalyReport.Query DefaultQuery()
        => new(TenantId: TenantId);

    // ── 1. Tenant sem utilizadores devolve relatório vazio ────────────────

    [Fact]
    public async Task Handle_NoUsers_ReturnsEmptyReport()
    {
        var result = await CreateHandler([]).Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalUsersAnalyzed.Should().Be(0);
        result.Value.TotalAnomalousUsers.Should().Be(0);
        result.Value.AnomalousUsers.Should().BeEmpty();
    }

    // ── 2. Utilizador normal sem anomalias não aparece no relatório ───────

    [Fact]
    public async Task Handle_NormalUser_NotInAnomalousUsers()
    {
        var result = await CreateHandler([NormalUser("user-ok")]).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.TotalUsersAnalyzed.Should().Be(1);
        result.Value.TotalAnomalousUsers.Should().Be(0);
    }

    // ── 3. Sinal OffHours detectado ───────────────────────────────────────

    [Fact]
    public async Task Handle_OffHoursRequest_DetectsOffHoursSignal()
    {
        var entry = NormalUser("user-offhours") with { OffHoursRequests = 5 };
        var result = await CreateHandler([entry]).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.TotalAnomalousUsers.Should().Be(1);
        var profile = result.Value.AnomalousUsers.Single();
        profile.DetectedSignals.Should().Contain(GetAccessPatternAnomalyReport.AnomalySignalType.OffHours);
        profile.RiskScore.Should().Be(10);
    }

    // ── 4. Sinal VolumetricSpike detectado ────────────────────────────────

    [Fact]
    public async Task Handle_VolumetricSpike_DetectsVolumetricSpikeSignal()
    {
        // MaxDailyRequests = 30, AvgDailyRequests = 5, multiplier default = 3 → 30 > 15 ✓
        var entry = NormalUser("user-spike") with
        {
            TotalRequests = 100,
            AvgDailyRequests = 5.0,
            MaxDailyRequests = 30.0
        };
        var result = await CreateHandler([entry]).Handle(DefaultQuery(), CancellationToken.None);

        var profile = result.Value.AnomalousUsers.Single();
        profile.DetectedSignals.Should().Contain(GetAccessPatternAnomalyReport.AnomalySignalType.VolumetricSpike);
        profile.RiskScore.Should().Be(25);
    }

    // ── 5. VolumetricSpike não activa com TotalRequests <= 5 ─────────────

    [Fact]
    public async Task Handle_VolumetricSpike_NotTriggeredWhenTotalRequestsBelowThreshold()
    {
        // MaxDailyRequests = 30 > 3*5=15, mas TotalRequests=4 → sinal NÃO activa
        var entry = NormalUser("user-low") with
        {
            TotalRequests = 4,
            AvgDailyRequests = 1.0,
            MaxDailyRequests = 30.0
        };
        var result = await CreateHandler([entry]).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.TotalAnomalousUsers.Should().Be(0);
    }

    // ── 6. Sinal FirstAccessSensitive detectado ───────────────────────────

    [Fact]
    public async Task Handle_SensitiveResourceAccess_DetectsFirstAccessSensitiveSignal()
    {
        var entry = NormalUser("user-sensitive") with { SensitiveResourceAccesses = 1 };
        var result = await CreateHandler([entry]).Handle(DefaultQuery(), CancellationToken.None);

        var profile = result.Value.AnomalousUsers.Single();
        profile.DetectedSignals.Should().Contain(GetAccessPatternAnomalyReport.AnomalySignalType.FirstAccessSensitive);
        profile.RiskScore.Should().Be(20);
    }

    // ── 7. Sinal UnusualResource detectado ────────────────────────────────

    [Fact]
    public async Task Handle_UnusualResourceAccess_DetectsUnusualResourceSignal()
    {
        var entry = NormalUser("user-unusual") with { UnusualResourceAccesses = 3 };
        var result = await CreateHandler([entry]).Handle(DefaultQuery(), CancellationToken.None);

        var profile = result.Value.AnomalousUsers.Single();
        profile.DetectedSignals.Should().Contain(GetAccessPatternAnomalyReport.AnomalySignalType.UnusualResource);
        profile.RiskScore.Should().Be(15);
    }

    // ── 8. Sinal BulkExport detectado ─────────────────────────────────────

    [Fact]
    public async Task Handle_BulkExportAboveThreshold_DetectsBulkExportSignal()
    {
        var entry = NormalUser("user-bulk") with { BulkExportCount = 25 }; // default threshold = 20
        var result = await CreateHandler([entry]).Handle(DefaultQuery(), CancellationToken.None);

        var profile = result.Value.AnomalousUsers.Single();
        profile.DetectedSignals.Should().Contain(GetAccessPatternAnomalyReport.AnomalySignalType.BulkExport);
        profile.RiskScore.Should().Be(30);
    }

    // ── 9. BulkExport não activa abaixo do threshold ──────────────────────

    [Fact]
    public async Task Handle_BulkExportBelowThreshold_NotDetected()
    {
        var entry = NormalUser("user-smallexport") with { BulkExportCount = 10 }; // default 20
        var result = await CreateHandler([entry]).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.TotalAnomalousUsers.Should().Be(0);
    }

    // ── 10. AnomalyDensityFlag activa com >= 3 sinais ─────────────────────

    [Fact]
    public async Task Handle_ThreeOrMoreSignals_AnomalyDensityFlagIsTrue()
    {
        // OffHours + FirstAccessSensitive + UnusualResource = 3 sinais
        var entry = NormalUser("user-density") with
        {
            OffHoursRequests = 1,
            SensitiveResourceAccesses = 1,
            UnusualResourceAccesses = 1
        };
        var result = await CreateHandler([entry]).Handle(DefaultQuery(), CancellationToken.None);

        var profile = result.Value.AnomalousUsers.Single();
        profile.AnomalyDensityFlag.Should().BeTrue();
        profile.AnomalySignalCount.Should().Be(3);
        result.Value.HighDensityUsers.Should().HaveCount(1);
    }

    // ── 11. AnomalyDensityFlag falso com < 3 sinais ───────────────────────

    [Fact]
    public async Task Handle_TwoSignals_AnomalyDensityFlagIsFalse()
    {
        var entry = NormalUser("user-two") with
        {
            OffHoursRequests = 1,
            SensitiveResourceAccesses = 1
        };
        var result = await CreateHandler([entry]).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AnomalousUsers.Single().AnomalyDensityFlag.Should().BeFalse();
        result.Value.HighDensityUsers.Should().BeEmpty();
    }

    // ── 12. RiskScore limitado a 100 ─────────────────────────────────────

    [Fact]
    public async Task Handle_AllSignals_RiskScoreCappedAt100()
    {
        // All 5 signals: 10+25+20+15+30 = 100
        var entry = new UserAccessEntry(
            "user-max", "user-max", "team-a",
            TotalRequests: 200,
            OffHoursRequests: 5,
            SensitiveResourceAccesses: 1,
            UnusualResourceAccesses: 1,
            BulkExportCount: 25,
            AvgDailyRequests: 1.0,
            MaxDailyRequests: 30.0);
        var result = await CreateHandler([entry]).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AnomalousUsers.Single().RiskScore.Should().Be(100);
    }

    // ── 13. SignalTypeDistribution conta correctamente ────────────────────

    [Fact]
    public async Task Handle_TwoOffHoursUsers_DistributionCountsCorrectly()
    {
        var entries = new[]
        {
            NormalUser("u1") with { OffHoursRequests = 1 },
            NormalUser("u2") with { OffHoursRequests = 1 }
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.SignalTypeDistribution["OffHours"].Should().Be(2);
    }

    // ── 14. AnomalousUsers ordenados por RiskScore descendente ───────────

    [Fact]
    public async Task Handle_MultipleAnomalousUsers_OrderedByRiskScoreDescending()
    {
        var entries = new[]
        {
            NormalUser("u-low") with { OffHoursRequests = 1 },                   // score=10
            NormalUser("u-high") with { OffHoursRequests = 1, BulkExportCount = 25 } // score=40
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.AnomalousUsers.First().UserId.Should().Be("u-high");
        result.Value.AnomalousUsers.Last().UserId.Should().Be("u-low");
    }

    // ── 15. Validator — TenantId obrigatório ─────────────────────────────

    [Fact]
    public void Validator_EmptyTenantId_FailsValidation()
    {
        var validator = new GetAccessPatternAnomalyReport.Validator();
        var result = validator.Validate(new GetAccessPatternAnomalyReport.Query(""));
        result.IsValid.Should().BeFalse();
    }

    // ── 16. Validator — LookbackDays fora do intervalo inválido ──────────

    [Fact]
    public void Validator_LookbackDaysOutOfRange_FailsValidation()
    {
        var validator = new GetAccessPatternAnomalyReport.Validator();
        var result = validator.Validate(new GetAccessPatternAnomalyReport.Query(TenantId, LookbackDays: 100));
        result.IsValid.Should().BeFalse();
    }
}

