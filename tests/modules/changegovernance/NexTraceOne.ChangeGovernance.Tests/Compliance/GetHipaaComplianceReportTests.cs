using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetHipaaComplianceReport;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

using GetHipaaFeature = NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetHipaaComplianceReport.GetHipaaComplianceReport;

namespace NexTraceOne.ChangeGovernance.Tests.Compliance;

/// <summary>
/// Testes unitários para Wave I.1 — HIPAA Security Rule Compliance Report.
/// Cobre avaliação de controlos § 164.312(a)(1), (b), (c)(1), (d) e (e)(1) e estado global.
/// </summary>
public sealed class GetHipaaComplianceReportTests
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

    private static Release MakeRelease(string serviceName = "healthcare-api")
        => Release.Create(TenantId, Guid.NewGuid(), serviceName, "2.0.0", "production", "jenkins", "def456", FixedNow);

    private static EvidencePack MakeEvidencePack(Guid releaseId, bool signed = false)
    {
        var pack = EvidencePack.Create(WorkflowInstanceId.New(), releaseId, FixedNow);
        if (signed) pack.ApplyIntegritySignature("{}", "hmac-hash", "auditor@healthcare.com", FixedNow);
        return pack;
    }

    // ── Core compliance tests ────────────────────────────────────────────────

    [Fact]
    public async Task GetHipaaComplianceReport_Returns_NotAssessed_When_No_Data()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetHipaaFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetHipaaFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be(Nis2ControlStatus.NotAssessed);
        result.Value.TotalReleases.Should().Be(0);
        result.Value.Controls.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetHipaaComplianceReport_Integrity_Compliant_When_All_Evidence_Signed()
    {
        var release = MakeRelease();
        var pack = MakeEvidencePack(release.Id.Value, signed: true);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[pack]);

        var handler = new GetHipaaFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetHipaaFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var integrityControl = result.Value.Controls.First(c => c.ControlId == "§ 164.312(c)(1)");
        integrityControl.Status.Should().Be(Nis2ControlStatus.Compliant);
    }

    [Fact]
    public async Task GetHipaaComplianceReport_Integrity_NonCompliant_When_No_Evidence_Signed()
    {
        var release = MakeRelease();
        var pack = MakeEvidencePack(release.Id.Value, signed: false);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[pack]);

        var handler = new GetHipaaFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetHipaaFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var integrityControl = result.Value.Controls.First(c => c.ControlId == "§ 164.312(c)(1)");
        integrityControl.Status.Should().Be(Nis2ControlStatus.NonCompliant);
    }

    [Fact]
    public async Task GetHipaaComplianceReport_Audit_Compliant_When_Releases_Exist()
    {
        var release = MakeRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetHipaaFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetHipaaFeature.Query(90), CancellationToken.None);

        var auditControl = result.Value.Controls.First(c => c.ControlId == "§ 164.312(b)");
        auditControl.Status.Should().Be(Nis2ControlStatus.Compliant);
    }

    [Fact]
    public async Task GetHipaaComplianceReport_AccessControl_Always_NotAssessed()
    {
        var release = MakeRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetHipaaFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetHipaaFeature.Query(90), CancellationToken.None);

        var accessControl = result.Value.Controls.First(c => c.ControlId == "§ 164.312(a)(1)");
        accessControl.Status.Should().Be(Nis2ControlStatus.NotAssessed);
    }

    [Fact]
    public async Task GetHipaaComplianceReport_TransmissionSecurity_Always_NotAssessed()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetHipaaFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetHipaaFeature.Query(90), CancellationToken.None);

        var transmissionControl = result.Value.Controls.First(c => c.ControlId == "§ 164.312(e)(1)");
        transmissionControl.Status.Should().Be(Nis2ControlStatus.NotAssessed);
    }

    [Fact]
    public async Task GetHipaaComplianceReport_FiltersByServiceName()
    {
        var releaseA = MakeRelease("healthcare-api");
        var releaseB = MakeRelease("billing-api");
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[releaseA, releaseB]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetHipaaFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetHipaaFeature.Query(90, "healthcare-api"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalReleases.Should().Be(1);
        result.Value.ServiceFilter.Should().Be("healthcare-api");
    }

    [Fact]
    public async Task GetHipaaComplianceReport_PartiallyCompliant_When_Some_Evidence_Signed()
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

        var handler = new GetHipaaFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetHipaaFeature.Query(90), CancellationToken.None);

        result.Value.SignedEvidencePacks.Should().Be(1);
        result.Value.TotalEvidencePacks.Should().Be(2);
        var integrityControl = result.Value.Controls.First(c => c.ControlId == "§ 164.312(c)(1)");
        integrityControl.Status.Should().Be(Nis2ControlStatus.PartiallyCompliant);
    }

    [Fact]
    public async Task GetHipaaComplianceReport_Overall_PartiallyCompliant_When_Auth_PartiallyCompliant()
    {
        var release = MakeRelease();
        var pack = MakeEvidencePack(release.Id.Value, signed: true);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[pack]);

        var handler = new GetHipaaFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetHipaaFeature.Query(90), CancellationToken.None);

        // Auth (§ 164.312(d)) will be PartiallyCompliant when releases exist → overall PartiallyCompliant
        result.Value.OverallStatus.Should().Be(Nis2ControlStatus.PartiallyCompliant);
    }

    [Fact]
    public async Task GetHipaaComplianceReport_Validator_Rejects_Invalid_Days()
    {
        var validator = new GetHipaaFeature.Validator();
        var resultLow = validator.Validate(new GetHipaaFeature.Query(0));
        var resultHigh = validator.Validate(new GetHipaaFeature.Query(366));

        resultLow.IsValid.Should().BeFalse();
        resultHigh.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetHipaaComplianceReport_Validator_Accepts_Valid_Days()
    {
        var validator = new GetHipaaFeature.Validator();
        var result = validator.Validate(new GetHipaaFeature.Query(90));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GetHipaaComplianceReport_Response_Has_Five_Controls()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetHipaaFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetHipaaFeature.Query(30), CancellationToken.None);

        result.Value.Controls.Should().HaveCount(5);
        result.Value.Controls.Select(c => c.ControlId).Should().Contain([
            "§ 164.312(a)(1)", "§ 164.312(b)", "§ 164.312(c)(1)", "§ 164.312(d)", "§ 164.312(e)(1)"
        ]);
    }
}
