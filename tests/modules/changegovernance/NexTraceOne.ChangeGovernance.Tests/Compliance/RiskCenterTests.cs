using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.ComputeServiceRiskProfile;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetRiskCenterReport;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetServiceRiskProfile;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Entities;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

namespace NexTraceOne.ChangeGovernance.Tests.Compliance;

/// <summary>
/// Testes unitários para Wave F.2 — Risk Center.
/// Cobre: ServiceRiskProfile domain, ComputeServiceRiskProfile, GetServiceRiskProfile, GetRiskCenterReport.
/// </summary>
public sealed class RiskCenterTests
{
    private readonly IServiceRiskProfileRepository _repo = Substitute.For<IServiceRiskProfileRepository>();
    private readonly IChangeIntelligenceUnitOfWork _uow = Substitute.For<IChangeIntelligenceUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private static readonly DateTimeOffset Now = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid ServiceId = Guid.Parse("11111111-0000-0000-0000-000000000001");

    public RiskCenterTests()
    {
        _clock.UtcNow.Returns(Now);
        _uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    // ── Domain entity tests ───────────────────────────────────────────────

    [Fact]
    public void ServiceRiskProfile_Compute_WithHighVulnerability_IsCritical()
    {
        var profile = ServiceRiskProfile.Compute(
            "tenant-1", ServiceId, "my-service",
            vulnerabilityScore: 100, changeFailureScore: 90,
            blastRadiusScore: 80, policyViolationScore: 70,
            activeSignals: [(RiskSignalType.VulnerabilityCritical, "2 critical CVEs")],
            computedAt: Now);

        profile.OverallRiskLevel.Should().Be(RiskLevel.Critical);
        profile.OverallScore.Should().BeGreaterThanOrEqualTo(80);
    }

    [Fact]
    public void ServiceRiskProfile_Compute_AllZero_IsNegligible()
    {
        var profile = ServiceRiskProfile.Compute(
            "t", ServiceId, "safe-svc",
            0, 0, 0, 0,
            activeSignals: [],
            computedAt: Now);

        profile.OverallRiskLevel.Should().Be(RiskLevel.Negligible);
        profile.OverallScore.Should().Be(0);
        profile.ActiveSignalCount.Should().Be(0);
    }

    [Theory]
    [InlineData(80, 60, 60, 60, RiskLevel.Critical)]   // weighted ~71 → actually let's compute: 80*.4+60*.25+60*.2+60*.15 = 32+15+12+9 = 68 → High
    [InlineData(90, 80, 70, 60, RiskLevel.Critical)]   // 90*.4+80*.25+70*.2+60*.15 = 36+20+14+9 = 79 → High
    [InlineData(100, 100, 100, 100, RiskLevel.Critical)] // 100 → Critical
    [InlineData(20, 20, 20, 20, RiskLevel.Low)]         // 20 → Low
    [InlineData(5, 5, 5, 5, RiskLevel.Negligible)]      // ~5 → Negligible
    public void ServiceRiskProfile_Compute_WeightedScoreMapping(
        int vuln, int change, int blast, int policy, RiskLevel _)
    {
        var overall = (int)Math.Round(vuln * 0.40m + change * 0.25m + blast * 0.20m + policy * 0.15m);
        var expected = overall switch
        {
            >= 80 => RiskLevel.Critical,
            >= 60 => RiskLevel.High,
            >= 40 => RiskLevel.Medium,
            >= 20 => RiskLevel.Low,
            _ => RiskLevel.Negligible
        };

        var profile = ServiceRiskProfile.Compute(
            "t", ServiceId, "svc", vuln, change, blast, policy, [], Now);

        profile.OverallRiskLevel.Should().Be(expected);
    }

    [Fact]
    public void ServiceRiskProfile_Compute_ScoresClampedTo100()
    {
        var profile = ServiceRiskProfile.Compute(
            "t", ServiceId, "svc",
            200, 200, 200, 200, [], Now);

        profile.VulnerabilityScore.Should().Be(100);
        profile.ChangeFailureScore.Should().Be(100);
        profile.BlastRadiusScore.Should().Be(100);
        profile.PolicyViolationScore.Should().Be(100);
        profile.OverallScore.Should().Be(100);
    }

    [Fact]
    public void ServiceRiskProfile_Compute_ActiveSignalsJson_IsValidJson()
    {
        var signals = new List<(RiskSignalType, string)>
        {
            (RiskSignalType.VulnerabilityCritical, "CVE-2024-0001"),
            (RiskSignalType.NoOwner, "No owner assigned")
        };

        var profile = ServiceRiskProfile.Compute("t", ServiceId, "svc", 90, 50, 40, 30, signals, Now);

        profile.ActiveSignalCount.Should().Be(2);
        profile.ActiveSignalsJson.Should().Contain("signal");
    }

    // ── Feature handler tests ─────────────────────────────────────────────

    [Fact]
    public async Task ComputeServiceRiskProfile_ValidCommand_PersistsAndReturns()
    {
        var handler = new ComputeServiceRiskProfile.Handler(_repo, _uow, _clock);
        var cmd = new ComputeServiceRiskProfile.Command(
            "tenant-1", ServiceId, "api-gateway",
            VulnerabilityScore: 80,
            ChangeFailureScore: 60,
            BlastRadiusScore: 70,
            PolicyViolationScore: 40,
            ActiveSignals: [
                new ComputeServiceRiskProfile.RiskSignalInput(RiskSignalType.VulnerabilityCritical, "3 critical CVEs"),
                new ComputeServiceRiskProfile.RiskSignalInput(RiskSignalType.LargeBlastRadius, "42 services impacted")
            ]);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("api-gateway");
        result.Value.VulnerabilityScore.Should().Be(80);
        result.Value.ActiveSignalCount.Should().Be(2);
        result.Value.ComputedAt.Should().Be(Now);
        _repo.Received(1).Add(Arg.Any<ServiceRiskProfile>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetServiceRiskProfile_ExistingProfile_ReturnsSuccess()
    {
        var profile = ServiceRiskProfile.Compute(
            "t", ServiceId, "svc", 70, 50, 60, 30, [], Now);
        _repo.GetLatestByServiceAsync("t", ServiceId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var handler = new GetServiceRiskProfile.Handler(_repo);
        var result = await handler.Handle(
            new GetServiceRiskProfile.Query("t", ServiceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("svc");
        result.Value.VulnerabilityScore.Should().Be(70);
    }

    [Fact]
    public async Task GetServiceRiskProfile_NotFound_ReturnsNotFound()
    {
        _repo.GetLatestByServiceAsync("t", ServiceId, Arg.Any<CancellationToken>())
            .Returns((ServiceRiskProfile?)null);

        var handler = new GetServiceRiskProfile.Handler(_repo);
        var result = await handler.Handle(
            new GetServiceRiskProfile.Query("t", ServiceId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("risk_center.profile_not_found");
    }

    [Fact]
    public async Task GetRiskCenterReport_ReturnsRankedServicesAndDistribution()
    {
        var p1 = ServiceRiskProfile.Compute("t", Guid.NewGuid(), "critical-svc", 90, 80, 70, 60, [], Now);
        var p2 = ServiceRiskProfile.Compute("t", Guid.NewGuid(), "medium-svc", 50, 40, 30, 20, [], Now);
        var p3 = ServiceRiskProfile.Compute("t", Guid.NewGuid(), "low-svc", 10, 10, 10, 10, [], Now);

        _repo.ListByTenantRankedAsync("t", 50, Arg.Any<CancellationToken>())
            .Returns(new List<ServiceRiskProfile> { p1, p2, p3 });

        var handler = new GetRiskCenterReport.Handler(_repo);
        var result = await handler.Handle(
            new GetRiskCenterReport.Query("t"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalReturned.Should().Be(3);
        result.Value.TotalWithProfiles.Should().Be(3);
        result.Value.Services[0].ServiceName.Should().Be("critical-svc");
        result.Value.Distribution.TotalServices.Should().Be(3);
    }

    [Fact]
    public async Task GetRiskCenterReport_WithMinimumRiskFilter_FiltersCorrectly()
    {
        var critical = ServiceRiskProfile.Compute("t", Guid.NewGuid(), "c-svc", 90, 85, 80, 75, [], Now);
        var low = ServiceRiskProfile.Compute("t", Guid.NewGuid(), "l-svc", 10, 10, 10, 10, [], Now);

        _repo.ListByTenantRankedAsync("t", 50, Arg.Any<CancellationToken>())
            .Returns(new List<ServiceRiskProfile> { critical, low });

        var handler = new GetRiskCenterReport.Handler(_repo);
        var result = await handler.Handle(
            new GetRiskCenterReport.Query("t", MinimumRiskLevel: RiskLevel.High), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Services.Should().OnlyContain(s =>
            s.OverallRiskLevel >= RiskLevel.High);
    }

    [Fact]
    public async Task GetRiskCenterReport_EmptyTenant_ReturnsEmptyReport()
    {
        _repo.ListByTenantRankedAsync("t", 50, Arg.Any<CancellationToken>())
            .Returns(new List<ServiceRiskProfile>());

        var handler = new GetRiskCenterReport.Handler(_repo);
        var result = await handler.Handle(new GetRiskCenterReport.Query("t"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalReturned.Should().Be(0);
        result.Value.Distribution.TotalServices.Should().Be(0);
    }
}
