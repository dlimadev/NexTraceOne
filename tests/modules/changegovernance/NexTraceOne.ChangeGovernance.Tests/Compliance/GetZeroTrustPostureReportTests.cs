using System.Linq;
using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetZeroTrustPostureReport;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

namespace NexTraceOne.ChangeGovernance.Tests.Compliance;

/// <summary>
/// Testes unitários para Wave AD.1 — GetZeroTrustPostureReport.
/// Cobre scoring por dimensão, ZeroTrustTier, CriticalExposure, TenantZeroTrustScore,
/// TierDistribution, TeamFilter e Validator.
/// </summary>
public sealed class GetZeroTrustPostureReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 22, 10, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-ad1";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static GetZeroTrustPostureReport.Handler CreateHandler(
        IReadOnlyList<ServiceSecurityEntry> entries)
    {
        var reader = Substitute.For<IZeroTrustServiceReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(entries));
        return new GetZeroTrustPostureReport.Handler(reader, CreateClock());
    }

    private static ServiceSecurityEntry MakeEntry(
        string name,
        string? team = "team-a",
        string tier = "Standard",
        bool auth = false,
        bool mtls = false,
        bool tokenRotation = false,
        int policies = 0)
        => new(name, name, team, tier, auth, mtls, tokenRotation, policies);

    private static GetZeroTrustPostureReport.Query DefaultQuery()
        => new(TenantId: TenantId);

    // ── 1. Tenant sem serviços devolve relatório vazio ────────────────────

    [Fact]
    public async Task Handle_NoServices_ReturnsEmptyReport()
    {
        var result = await CreateHandler([]).Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServicesAnalyzed.Should().Be(0);
        result.Value.TenantZeroTrustScore.Should().Be(0.0);
        result.Value.ServiceProfiles.Should().BeEmpty();
        result.Value.CriticalExposureCount.Should().Be(0);
    }

    // ── 2. Score = 100 quando todas as dimensões estão presentes ──────────

    [Fact]
    public async Task Handle_AllDimensionsPresent_ScoreIs100()
    {
        var entry = MakeEntry("svc-full", auth: true, mtls: true, tokenRotation: true, policies: 2);
        var result = await CreateHandler([entry]).Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var profile = result.Value.ServiceProfiles.Single();
        profile.ZeroTrustScore.Should().Be(100);
        profile.Tier.Should().Be(ZeroTrustTier.Enforced);
    }

    // ── 3. Score = 0 quando nenhuma dimensão está presente (Exposed) ──────

    [Fact]
    public async Task Handle_NoDimensions_ScoreIs0_TierIsExposed()
    {
        var entry = MakeEntry("svc-none");
        var result = await CreateHandler([entry]).Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var profile = result.Value.ServiceProfiles.Single();
        profile.ZeroTrustScore.Should().Be(0);
        profile.Tier.Should().Be(ZeroTrustTier.Exposed);
    }

    // ── 4. Tier Controlled (score 65–84): auth + mtls + policies = 80 ─────

    [Fact]
    public async Task Handle_ScoreOf80_TierIsControlled()
    {
        // auth(30) + mtls(25) + policies(25) = 80
        var entry = MakeEntry("svc-controlled", auth: true, mtls: true, policies: 1);
        var result = await CreateHandler([entry]).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.ServiceProfiles.Single().Tier.Should().Be(ZeroTrustTier.Controlled);
    }

    // ── 5. Tier Partial (score 40–64): apenas auth + tokenRotation = 50 ───

    [Fact]
    public async Task Handle_ScoreOf50_TierIsPartial()
    {
        // auth(30) + tokenRotation(20) = 50
        var entry = MakeEntry("svc-partial", auth: true, tokenRotation: true);
        var result = await CreateHandler([entry]).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.ServiceProfiles.Single().Tier.Should().Be(ZeroTrustTier.Partial);
    }

    // ── 6. CriticalExposure flag para serviço Critical com Exposed ─────────

    [Fact]
    public async Task Handle_CriticalServiceWithExposed_FlagsCriticalExposure()
    {
        var entry = MakeEntry("svc-critical", tier: "Critical"); // score=0, Exposed
        var result = await CreateHandler([entry]).Handle(DefaultQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceProfiles.Single().CriticalExposure.Should().BeTrue();
        result.Value.CriticalExposureCount.Should().Be(1);
    }

    // ── 7. Standard+Exposed não é CriticalExposure ────────────────────────

    [Fact]
    public async Task Handle_StandardServiceWithExposed_NoCriticalExposure()
    {
        var entry = MakeEntry("svc-standard", tier: "Standard"); // score=0, Exposed
        var result = await CreateHandler([entry]).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.ServiceProfiles.Single().CriticalExposure.Should().BeFalse();
        result.Value.CriticalExposureCount.Should().Be(0);
    }

    // ── 8. TenantZeroTrustScore ponderado por tier ────────────────────────

    [Fact]
    public async Task Handle_MultipleServices_TenantScoreIsWeighted()
    {
        // Critical(score=100, weight=3) + Standard(score=0, weight=2) + Experimental(score=100, weight=1)
        var entries = new[]
        {
            MakeEntry("svc-crit", tier: "Critical", auth: true, mtls: true, tokenRotation: true, policies: 1),
            MakeEntry("svc-std", tier: "Standard"),
            MakeEntry("svc-exp", tier: "Experimental", auth: true, mtls: true, tokenRotation: true, policies: 1)
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        // (100*3 + 0*2 + 100*1) / (3+2+1) = 400/6 ≈ 66.67
        result.Value.TenantZeroTrustScore.Should().BeApproximately(66.67, 0.01);
    }

    // ── 9. TierDistribution correcta ──────────────────────────────────────

    [Fact]
    public async Task Handle_MultipleServices_TierDistributionIsCorrect()
    {
        var entries = new[]
        {
            MakeEntry("s1", auth: true, mtls: true, tokenRotation: true, policies: 1), // 100, Enforced
            MakeEntry("s2", auth: true, mtls: true, policies: 1),                      // 80, Controlled
            MakeEntry("s3", auth: true, tokenRotation: true),                          // 50, Partial
            MakeEntry("s4")                                                            // 0, Exposed
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        var dist = result.Value.TierDistribution;
        dist.EnforcedCount.Should().Be(1);
        dist.ControlledCount.Should().Be(1);
        dist.PartialCount.Should().Be(1);
        dist.ExposedCount.Should().Be(1);
    }

    // ── 10. TopExposedServices ordena por score crescente ─────────────────

    [Fact]
    public async Task Handle_MultipleServices_TopExposedOrderedByScoreAscending()
    {
        var entries = new[]
        {
            MakeEntry("svc-high", auth: true, mtls: true, tokenRotation: true, policies: 1), // 100
            MakeEntry("svc-low"),                                                             // 0
            MakeEntry("svc-mid", auth: true, tokenRotation: true)                            // 50
        };
        var result = await CreateHandler(entries).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.TopExposedServices.First().ServiceName.Should().Be("svc-low");
        result.Value.TopExposedServices.Last().ServiceName.Should().Be("svc-high");
    }

    // ── 11. TeamFilter aplica filtro por equipa ───────────────────────────

    [Fact]
    public async Task Handle_TeamFilter_ReturnsOnlyMatchingTeam()
    {
        var entries = new[]
        {
            MakeEntry("svc-a", team: "team-alpha"),
            MakeEntry("svc-b", team: "team-beta")
        };
        var query = new GetZeroTrustPostureReport.Query(TenantId: TenantId, TeamFilter: "team-alpha");
        var result = await CreateHandler(entries).Handle(query, CancellationToken.None);

        result.Value.TotalServicesAnalyzed.Should().Be(1);
        result.Value.ServiceProfiles.Single().ServiceName.Should().Be("svc-a");
    }

    // ── 12. GeneratedAt é o timestamp do clock ─────────────────────────────

    [Fact]
    public async Task Handle_ReturnsCorrectGeneratedAt()
    {
        var result = await CreateHandler([]).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.GeneratedAt.Should().Be(FixedNow);
    }

    // ── 13. Validator — TenantId obrigatório ──────────────────────────────

    [Fact]
    public void Validator_EmptyTenantId_FailsValidation()
    {
        var validator = new GetZeroTrustPostureReport.Validator();
        var result = validator.Validate(new GetZeroTrustPostureReport.Query(""));
        result.IsValid.Should().BeFalse();
    }

    // ── 14. Validator — MaxServices fora do intervalo inválido ───────────

    [Fact]
    public void Validator_MaxServicesOutOfRange_FailsValidation()
    {
        var validator = new GetZeroTrustPostureReport.Validator();
        var result = validator.Validate(new GetZeroTrustPostureReport.Query(TenantId, MaxServices: 0));
        result.IsValid.Should().BeFalse();
    }

    // ── 15. Pontuação dimensional correcta por dimensão isolada ──────────

    [Theory]
    [InlineData(true, false, false, 0, 30)]  // auth only
    [InlineData(false, true, false, 0, 25)]  // mtls only
    [InlineData(false, false, true, 0, 20)]  // tokenRotation only
    [InlineData(false, false, false, 1, 25)] // policy only
    public async Task Handle_SingleDimension_ScoreMatchesWeight(
        bool auth, bool mtls, bool token, int policies, int expectedScore)
    {
        var entry = MakeEntry("svc-dim", auth: auth, mtls: mtls, tokenRotation: token, policies: policies);
        var result = await CreateHandler([entry]).Handle(DefaultQuery(), CancellationToken.None);

        result.Value.ServiceProfiles.Single().ZeroTrustScore.Should().Be(expectedScore);
    }
}
