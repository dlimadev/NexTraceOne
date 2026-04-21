using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Workflow.Features.GetEvidencePackCoverageReport;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

namespace NexTraceOne.ChangeGovernance.Tests.Workflow.Application.Features;

/// <summary>
/// Testes unitários para Wave N.3 — GetEvidencePackCoverageReport.
/// Cobre taxa de cobertura de evidence packs por releases, breakdown por ambiente,
/// packs assinados, completude e lista de releases sem cobertura.
/// </summary>
public sealed class EvidencePackCoverageReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static Release MakeRelease(string service, string env, string version = "1.0.0")
        => Release.Create(TenantId, Guid.NewGuid(), service, version, env,
            "jenkins", "abc123", FixedNow.AddDays(-10));

    private static EvidencePack MakePack(Guid releaseId, bool signed = false, decimal completeness = 100m)
    {
        var pack = EvidencePack.Create(WorkflowInstanceId.New(), releaseId, FixedNow);
        // Patch completeness via UpdateScores to reach desired level
        if (completeness >= 50m)
            pack.UpdateScores(0.5m, 0.5m, 0.5m);
        if (signed)
            pack.ApplyIntegritySignature("{}", "hash123", "auditor", FixedNow);
        return pack;
    }

    // ── Empty report ──────────────────────────────────────────────────────

    [Fact]
    public async Task Report_Empty_When_No_Releases()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var packRepo = Substitute.For<IEvidencePackRepository>();

        releaseRepo.ListInRangeAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetEvidencePackCoverageReport.Handler(releaseRepo, packRepo, CreateClock());
        var result = await handler.Handle(
            new GetEvidencePackCoverageReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalReleases.Should().Be(0);
        result.Value.CoveragePercent.Should().Be(0m);
        result.Value.UncoveredReleases.Should().BeEmpty();
    }

    // ── All releases have packs → 100% coverage ───────────────────────────

    [Fact]
    public async Task CoveragePercent_100_WhenAllReleasesHavePacks()
    {
        var r1 = MakeRelease("svc-a", "prod");
        var r2 = MakeRelease("svc-b", "prod");
        var releases = new[] { r1, r2 };

        var p1 = MakePack(r1.Id.Value);
        var p2 = MakePack(r2.Id.Value);

        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(releases);

        var packRepo = Substitute.For<IEvidencePackRepository>();
        packRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { p1, p2 });

        var handler = new GetEvidencePackCoverageReport.Handler(releaseRepo, packRepo, CreateClock());
        var result = await handler.Handle(
            new GetEvidencePackCoverageReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CoveragePercent.Should().Be(100m);
        result.Value.ReleasesWithPack.Should().Be(2);
        result.Value.ReleasesWithoutPack.Should().Be(0);
        result.Value.UncoveredReleases.Should().BeEmpty();
    }

    // ── Partial coverage ──────────────────────────────────────────────────

    [Fact]
    public async Task CoveragePercent_50_WhenHalfReleasesHavePacks()
    {
        var r1 = MakeRelease("svc-a", "prod");
        var r2 = MakeRelease("svc-b", "prod");
        var releases = new[] { r1, r2 };

        var p1 = MakePack(r1.Id.Value);  // only r1 has a pack

        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(releases);

        var packRepo = Substitute.For<IEvidencePackRepository>();
        packRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { p1 });

        var handler = new GetEvidencePackCoverageReport.Handler(releaseRepo, packRepo, CreateClock());
        var result = await handler.Handle(
            new GetEvidencePackCoverageReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CoveragePercent.Should().Be(50m);
        result.Value.ReleasesWithPack.Should().Be(1);
        result.Value.ReleasesWithoutPack.Should().Be(1);
        result.Value.UncoveredReleases.Should().HaveCount(1);
        result.Value.UncoveredReleases[0].ServiceName.Should().Be("svc-b");
    }

    // ── SignedPackPercent ─────────────────────────────────────────────────

    [Fact]
    public async Task SignedPackPercent_CorrectWhen_HalfSigned()
    {
        var r1 = MakeRelease("svc-a", "prod");
        var r2 = MakeRelease("svc-b", "prod");
        var releases = new[] { r1, r2 };

        var p1 = MakePack(r1.Id.Value, signed: true);
        var p2 = MakePack(r2.Id.Value, signed: false);

        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(releases);

        var packRepo = Substitute.For<IEvidencePackRepository>();
        packRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { p1, p2 });

        var handler = new GetEvidencePackCoverageReport.Handler(releaseRepo, packRepo, CreateClock());
        var result = await handler.Handle(
            new GetEvidencePackCoverageReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SignedPackPercent.Should().Be(50m);
    }

    // ── ByEnvironment breakdown ───────────────────────────────────────────

    [Fact]
    public async Task ByEnvironment_BreakdownIsCorrect()
    {
        var rProd = MakeRelease("svc-a", "production");
        var rStag = MakeRelease("svc-b", "staging");
        var releases = new[] { rProd, rStag };

        // Only prod has a pack
        var pProd = MakePack(rProd.Id.Value);

        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(releases);

        var packRepo = Substitute.For<IEvidencePackRepository>();
        packRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { pProd });

        var handler = new GetEvidencePackCoverageReport.Handler(releaseRepo, packRepo, CreateClock());
        var result = await handler.Handle(
            new GetEvidencePackCoverageReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByEnvironment.Should().HaveCount(2);

        var prod = result.Value.ByEnvironment.Single(e => e.Environment == "production");
        prod.CoveragePercent.Should().Be(100m);
        prod.ReleasesWithPack.Should().Be(1);

        var stag = result.Value.ByEnvironment.Single(e => e.Environment == "staging");
        stag.CoveragePercent.Should().Be(0m);
        stag.ReleasesWithoutPack.Should().Be(1);
    }

    // ── MaxUncoveredReleases cap ──────────────────────────────────────────

    [Fact]
    public async Task MaxUncoveredReleases_Cap_IsRespected()
    {
        var releases = Enumerable.Range(1, 10)
            .Select(i => MakeRelease($"svc-{i:D2}", "prod"))
            .ToList();

        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(releases);

        var packRepo = Substitute.For<IEvidencePackRepository>();
        packRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);  // No packs — all uncovered

        var handler = new GetEvidencePackCoverageReport.Handler(releaseRepo, packRepo, CreateClock());
        var result = await handler.Handle(
            new GetEvidencePackCoverageReport.Query(TenantId, MaxUncoveredReleases: 5),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UncoveredReleases.Count.Should().Be(5);
    }

    // ── Report metadata ───────────────────────────────────────────────────

    [Fact]
    public async Task Report_Contains_Correct_Period()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        releaseRepo.ListInRangeAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns([]);

        var packRepo = Substitute.For<IEvidencePackRepository>();

        var handler = new GetEvidencePackCoverageReport.Handler(releaseRepo, packRepo, CreateClock());
        var result = await handler.Handle(
            new GetEvidencePackCoverageReport.Query(TenantId, LookbackDays: 14),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(TenantId);
        result.Value.LookbackDays.Should().Be(14);
        result.Value.From.Should().BeCloseTo(FixedNow.AddDays(-14), TimeSpan.FromSeconds(1));
        result.Value.GeneratedAt.Should().Be(FixedNow);
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public void Validator_Rejects_EmptyTenantId()
    {
        var validator = new GetEvidencePackCoverageReport.Validator();
        var result = validator.Validate(new GetEvidencePackCoverageReport.Query(Guid.Empty));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Validator_Rejects_InvalidLookbackDays()
    {
        var validator = new GetEvidencePackCoverageReport.Validator();
        var result = validator.Validate(
            new GetEvidencePackCoverageReport.Query(TenantId, LookbackDays: 0));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LookbackDays");
    }

    [Fact]
    public void Validator_Rejects_InvalidMaxUncoveredReleases()
    {
        var validator = new GetEvidencePackCoverageReport.Validator();
        var result = validator.Validate(
            new GetEvidencePackCoverageReport.Query(TenantId, MaxUncoveredReleases: 0));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaxUncoveredReleases");
    }

    [Fact]
    public void Validator_Rejects_InvalidCompletenessThreshold()
    {
        var validator = new GetEvidencePackCoverageReport.Validator();
        var result = validator.Validate(
            new GetEvidencePackCoverageReport.Query(TenantId, CompletenessThreshold: -1m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CompletenessThreshold");
    }

    [Fact]
    public void Validator_Accepts_Valid_Query()
    {
        var validator = new GetEvidencePackCoverageReport.Validator();
        var result = validator.Validate(
            new GetEvidencePackCoverageReport.Query(TenantId,
                LookbackDays: 30, MaxUncoveredReleases: 20, CompletenessThreshold: 80m));
        result.IsValid.Should().BeTrue();
    }
}
