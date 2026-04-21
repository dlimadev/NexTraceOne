using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetNis2ComplianceReport;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

using GetNis2ComplianceReportFeature = NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetNis2ComplianceReport.GetNis2ComplianceReport;

namespace NexTraceOne.ChangeGovernance.Tests.Compliance;

/// <summary>
/// Testes unitários para Wave C.2 — NIS2 Compliance Report.
/// Cobre avaliação de controlos RCM-1 a RCM-5 e determinação de estado global.
/// </summary>
public sealed class GetNis2ComplianceReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);
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

    private static Release MakeRelease(string serviceName = "order-service", string environment = "production")
        => Release.Create(TenantId, Guid.NewGuid(), serviceName, "1.0.0", environment, "github-actions", "abc123", FixedNow);

    private static EvidencePack MakeEvidencePack(Guid releaseId, bool signed = false)
    {
        var pack = EvidencePack.Create(WorkflowInstanceId.New(), releaseId, FixedNow);
        if (signed) pack.ApplyIntegritySignature("{}", "hash123", "auditor@test.com", FixedNow);
        return pack;
    }

    // ── Tests ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetNis2ComplianceReport_Returns_NotAssessed_When_No_Data()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetNis2ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetNis2ComplianceReportFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be(Nis2ControlStatus.NotAssessed);
    }

    [Fact]
    public async Task GetNis2ComplianceReport_RCM1_Compliant_When_Releases_Exist()
    {
        var release = MakeRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetNis2ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetNis2ComplianceReportFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var rcm1 = result.Value.Controls.Single(c => c.ControlId == "RCM-1");
        rcm1.Status.Should().Be(Nis2ControlStatus.Compliant);
    }

    [Fact]
    public async Task GetNis2ComplianceReport_RCM2_Compliant_When_All_Signed()
    {
        var release = MakeRelease();
        var pack = MakeEvidencePack(release.Id.Value, signed: true);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[pack]);

        var handler = new GetNis2ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetNis2ComplianceReportFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var rcm2 = result.Value.Controls.Single(c => c.ControlId == "RCM-2");
        rcm2.Status.Should().Be(Nis2ControlStatus.Compliant);
        result.Value.SignedEvidencePacks.Should().Be(1);
    }

    [Fact]
    public async Task GetNis2ComplianceReport_RCM2_PartiallyCompliant_When_Some_Signed()
    {
        var release1 = MakeRelease("svc-a");
        var release2 = MakeRelease("svc-b");
        var packSigned = MakeEvidencePack(release1.Id.Value, signed: true);
        var packUnsigned = MakeEvidencePack(release2.Id.Value, signed: false);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release1, release2]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[packSigned, packUnsigned]);

        var handler = new GetNis2ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetNis2ComplianceReportFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var rcm2 = result.Value.Controls.Single(c => c.ControlId == "RCM-2");
        rcm2.Status.Should().Be(Nis2ControlStatus.PartiallyCompliant);
    }

    [Fact]
    public async Task GetNis2ComplianceReport_RCM2_NonCompliant_When_None_Signed()
    {
        var release = MakeRelease();
        var packUnsigned = MakeEvidencePack(release.Id.Value, signed: false);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[packUnsigned]);

        var handler = new GetNis2ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetNis2ComplianceReportFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var rcm2 = result.Value.Controls.Single(c => c.ControlId == "RCM-2");
        rcm2.Status.Should().Be(Nis2ControlStatus.NonCompliant);
    }

    [Fact]
    public async Task GetNis2ComplianceReport_Overall_NonCompliant_When_Any_Control_NonCompliant()
    {
        var release = MakeRelease();
        var packUnsigned = MakeEvidencePack(release.Id.Value, signed: false);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[packUnsigned]);

        var handler = new GetNis2ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetNis2ComplianceReportFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be(Nis2ControlStatus.NonCompliant);
    }

    [Fact]
    public async Task GetNis2ComplianceReport_Overall_PartiallyCompliant_When_Any_Partial()
    {
        var release1 = MakeRelease("svc-a");
        var release2 = MakeRelease("svc-b");
        var packSigned = MakeEvidencePack(release1.Id.Value, signed: true);
        var packUnsigned = MakeEvidencePack(release2.Id.Value, signed: false);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release1, release2]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[packSigned, packUnsigned]);

        var handler = new GetNis2ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetNis2ComplianceReportFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be(Nis2ControlStatus.PartiallyCompliant);
    }

    [Fact]
    public async Task GetNis2ComplianceReport_Overall_Compliant_When_All_Compliant()
    {
        var release = MakeRelease();
        var packSigned = MakeEvidencePack(release.Id.Value, signed: true);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[packSigned]);

        var handler = new GetNis2ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetNis2ComplianceReportFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // RCM-3 and RCM-4 are NotAssessed, so overall won't be Compliant unless only assessed ones are Compliant
        // (DetermineOverall: if all NotAssessed → NotAssessed; else Compliant)
        // Here: RCM-1=Compliant, RCM-2=Compliant, RCM-3=NotAssessed, RCM-4=NotAssessed, RCM-5=Compliant
        // Not all NotAssessed, no NonCompliant, no PartiallyCompliant → Compliant
        result.Value.OverallStatus.Should().Be(Nis2ControlStatus.Compliant);
    }

    [Fact]
    public async Task GetNis2ComplianceReport_Filters_By_ServiceName()
    {
        var releaseA = MakeRelease("order-service");
        var releaseB = MakeRelease("payment-service");
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[releaseA, releaseB]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetNis2ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetNis2ComplianceReportFeature.Query(90, "order-service"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalReleases.Should().Be(1);
        result.Value.ServiceFilter.Should().Be("order-service");
    }

    [Fact]
    public async Task GetNis2ComplianceReport_Returns_All_Five_Controls()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetNis2ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetNis2ComplianceReportFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Controls.Should().HaveCount(5);
        result.Value.Controls.Select(c => c.ControlId).Should().BeEquivalentTo(["RCM-1", "RCM-2", "RCM-3", "RCM-4", "RCM-5"]);
    }

    [Fact]
    public async Task GetNis2ComplianceReport_Has_Correct_Period_In_Response()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetNis2ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetNis2ComplianceReportFeature.Query(180), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PeriodDays.Should().Be(180);
        result.Value.GeneratedAt.Should().Be(FixedNow);
    }

    [Fact]
    public void Nis2ControlStatus_Enum_Has_Expected_Values()
    {
        ((int)Nis2ControlStatus.NotAssessed).Should().Be(0);
        ((int)Nis2ControlStatus.Compliant).Should().Be(1);
        ((int)Nis2ControlStatus.PartiallyCompliant).Should().Be(2);
        ((int)Nis2ControlStatus.NonCompliant).Should().Be(3);
    }
}
