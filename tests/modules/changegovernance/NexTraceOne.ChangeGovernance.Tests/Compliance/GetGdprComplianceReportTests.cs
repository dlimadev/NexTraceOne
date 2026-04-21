using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetGdprComplianceReport;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

using GetGdprFeature = NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetGdprComplianceReport.GetGdprComplianceReport;

namespace NexTraceOne.ChangeGovernance.Tests.Compliance;

/// <summary>
/// Testes unitários para Wave J.1 — GDPR Compliance Report.
/// Cobre avaliação de controlos Art. 5, 13, 17, 25 e 33 e estado global.
/// </summary>
public sealed class GetGdprComplianceReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private static ICurrentTenant CreateTenant()
    {
        var tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(TenantId);
        return tenant;
    }

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static Release MakeRelease(string serviceName = "data-platform")
        => Release.Create(TenantId, Guid.NewGuid(), serviceName, "1.0.0", "production", "jenkins", "abc123", FixedNow);

    private static EvidencePack MakeEvidencePack(Guid releaseId, bool signed = false)
    {
        var pack = EvidencePack.Create(WorkflowInstanceId.New(), releaseId, FixedNow);
        if (signed) pack.ApplyIntegritySignature("{}", "hmac-hash", "dpo@example.com", FixedNow);
        return pack;
    }

    // ── Core compliance tests ────────────────────────────────────────────────

    [Fact]
    public async Task GetGdprComplianceReport_Returns_NotAssessed_When_No_Data()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetGdprFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetGdprFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be(Nis2ControlStatus.NotAssessed);
        result.Value.TotalReleases.Should().Be(0);
        result.Value.Controls.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetGdprComplianceReport_Has_Five_Controls()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetGdprFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetGdprFeature.Query(30), CancellationToken.None);

        result.Value.Controls.Should().HaveCount(5);
        result.Value.Controls.Select(c => c.ArticleId).Should().Contain(["Art. 5", "Art. 13", "Art. 17", "Art. 25", "Art. 33"]);
    }

    [Fact]
    public async Task GetGdprComplianceReport_Art17_Always_NotAssessed()
    {
        var release = MakeRelease();
        var pack = MakeEvidencePack(release.Id.Value, signed: true);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[pack]);

        var handler = new GetGdprFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetGdprFeature.Query(90), CancellationToken.None);

        var art17 = result.Value.Controls.First(c => c.ArticleId == "Art. 17");
        art17.Status.Should().Be(Nis2ControlStatus.NotAssessed);
    }

    [Fact]
    public async Task GetGdprComplianceReport_Art25_Compliant_When_All_Evidence_Signed()
    {
        var release = MakeRelease();
        var pack = MakeEvidencePack(release.Id.Value, signed: true);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[pack]);

        var handler = new GetGdprFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetGdprFeature.Query(90), CancellationToken.None);

        var art25 = result.Value.Controls.First(c => c.ArticleId == "Art. 25");
        art25.Status.Should().Be(Nis2ControlStatus.Compliant);
    }

    [Fact]
    public async Task GetGdprComplianceReport_Art25_NonCompliant_When_No_Evidence_Signed()
    {
        var release = MakeRelease();
        var pack = MakeEvidencePack(release.Id.Value, signed: false);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[pack]);

        var handler = new GetGdprFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetGdprFeature.Query(90), CancellationToken.None);

        var art25 = result.Value.Controls.First(c => c.ArticleId == "Art. 25");
        art25.Status.Should().Be(Nis2ControlStatus.NonCompliant);
    }

    [Fact]
    public async Task GetGdprComplianceReport_Art5_PartiallyCompliant_When_Releases_Exist()
    {
        var release = MakeRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetGdprFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetGdprFeature.Query(90), CancellationToken.None);

        var art5 = result.Value.Controls.First(c => c.ArticleId == "Art. 5");
        art5.Status.Should().Be(Nis2ControlStatus.PartiallyCompliant);
    }

    [Fact]
    public async Task GetGdprComplianceReport_Art33_PartiallyCompliant_When_Releases_Exist()
    {
        var release = MakeRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetGdprFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetGdprFeature.Query(90), CancellationToken.None);

        var art33 = result.Value.Controls.First(c => c.ArticleId == "Art. 33");
        art33.Status.Should().Be(Nis2ControlStatus.PartiallyCompliant);
    }

    [Fact]
    public async Task GetGdprComplianceReport_Overall_PartiallyCompliant_When_Releases_Exist()
    {
        var release = MakeRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetGdprFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetGdprFeature.Query(90), CancellationToken.None);

        result.Value.OverallStatus.Should().Be(Nis2ControlStatus.PartiallyCompliant);
    }

    [Fact]
    public async Task GetGdprComplianceReport_FiltersByServiceName()
    {
        var releaseA = MakeRelease("data-platform");
        var releaseB = MakeRelease("auth-service");
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[releaseA, releaseB]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetGdprFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetGdprFeature.Query(90, "data-platform"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalReleases.Should().Be(1);
        result.Value.ServiceFilter.Should().Be("data-platform");
    }

    [Fact]
    public async Task GetGdprComplianceReport_Art25_PartiallyCompliant_When_Some_Evidence_Signed()
    {
        var release = MakeRelease();
        var packSigned = MakeEvidencePack(release.Id.Value, signed: true);
        var packUnsigned = MakeEvidencePack(release.Id.Value, signed: false);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[packSigned, packUnsigned]);

        var handler = new GetGdprFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetGdprFeature.Query(90), CancellationToken.None);

        var art25 = result.Value.Controls.First(c => c.ArticleId == "Art. 25");
        art25.Status.Should().Be(Nis2ControlStatus.PartiallyCompliant);
        result.Value.SignedEvidencePacks.Should().Be(1);
        result.Value.TotalEvidencePacks.Should().Be(2);
    }

    [Fact]
    public async Task GetGdprComplianceReport_Validator_Rejects_Invalid_Days()
    {
        var validator = new GetGdprFeature.Validator();
        var resultLow = validator.Validate(new GetGdprFeature.Query(0));
        var resultHigh = validator.Validate(new GetGdprFeature.Query(366));

        resultLow.IsValid.Should().BeFalse();
        resultHigh.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetGdprComplianceReport_Validator_Accepts_Valid_Days()
    {
        var validator = new GetGdprFeature.Validator();
        var result = validator.Validate(new GetGdprFeature.Query(90));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GetGdprComplianceReport_Response_Contains_Expected_Control_Names()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetGdprFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetGdprFeature.Query(90), CancellationToken.None);

        result.Value.Controls.Select(c => c.ControlName).Should().Contain([
            "Principles of Processing",
            "Transparency",
            "Right to Erasure",
            "Privacy by Design",
            "Breach Notification Readiness"
        ]);
    }
}
