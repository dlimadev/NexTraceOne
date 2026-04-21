using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetSoc2ComplianceReport;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

using GetSoc2ComplianceReportFeature = NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetSoc2ComplianceReport.GetSoc2ComplianceReport;

namespace NexTraceOne.ChangeGovernance.Tests.Compliance;

/// <summary>
/// Testes unitários para Wave G.1 — SOC 2 Compliance Report.
/// Cobre avaliação de controlos CC6, CC7, CC8, CC9 e A1, e determinação de estado global.
/// </summary>
public sealed class GetSoc2ComplianceReportTests
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

    // ── Tests ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSoc2ComplianceReport_Returns_NotAssessed_When_No_Data()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetSoc2ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetSoc2ComplianceReportFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be(Nis2ControlStatus.NotAssessed);
    }

    [Fact]
    public async Task GetSoc2ComplianceReport_CC7_Compliant_When_Releases_Exist()
    {
        var release = MakeRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetSoc2ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetSoc2ComplianceReportFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var cc7 = result.Value.Controls.Single(c => c.ControlId == "CC7");
        cc7.Status.Should().Be(Nis2ControlStatus.Compliant);
    }

    [Fact]
    public async Task GetSoc2ComplianceReport_CC8_Compliant_When_All_Evidence_Signed()
    {
        var release = MakeRelease();
        var pack = MakeEvidencePack(release.Id.Value, signed: true);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[pack]);

        var handler = new GetSoc2ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetSoc2ComplianceReportFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var cc8 = result.Value.Controls.Single(c => c.ControlId == "CC8");
        cc8.Status.Should().Be(Nis2ControlStatus.Compliant);
    }

    [Fact]
    public async Task GetSoc2ComplianceReport_CC8_NonCompliant_When_No_Evidence_Signed()
    {
        var release = MakeRelease();
        var pack = MakeEvidencePack(release.Id.Value, signed: false);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[pack]);

        var handler = new GetSoc2ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetSoc2ComplianceReportFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var cc8 = result.Value.Controls.Single(c => c.ControlId == "CC8");
        cc8.Status.Should().Be(Nis2ControlStatus.NonCompliant);
    }

    [Fact]
    public async Task GetSoc2ComplianceReport_CC8_PartiallyCompliant_When_Some_Evidence_Signed()
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

        var handler = new GetSoc2ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetSoc2ComplianceReportFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var cc8 = result.Value.Controls.Single(c => c.ControlId == "CC8");
        cc8.Status.Should().Be(Nis2ControlStatus.PartiallyCompliant);
    }

    [Fact]
    public async Task GetSoc2ComplianceReport_CC6_And_CC9_Are_NotAssessed()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetSoc2ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetSoc2ComplianceReportFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Controls.Single(c => c.ControlId == "CC6").Status.Should().Be(Nis2ControlStatus.NotAssessed);
        result.Value.Controls.Single(c => c.ControlId == "CC9").Status.Should().Be(Nis2ControlStatus.NotAssessed);
    }

    [Fact]
    public async Task GetSoc2ComplianceReport_Returns_All_Five_Controls()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetSoc2ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetSoc2ComplianceReportFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Controls.Should().HaveCount(5);
        result.Value.Controls.Select(c => c.ControlId).Should().BeEquivalentTo(["CC6", "CC7", "CC8", "CC9", "A1"]);
    }

    [Fact]
    public async Task GetSoc2ComplianceReport_Overall_NonCompliant_When_Any_NonCompliant()
    {
        var release = MakeRelease();
        var packUnsigned = MakeEvidencePack(release.Id.Value, signed: false);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[packUnsigned]);

        var handler = new GetSoc2ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetSoc2ComplianceReportFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be(Nis2ControlStatus.NonCompliant);
    }

    [Fact]
    public async Task GetSoc2ComplianceReport_Filters_By_ServiceName()
    {
        var releaseA = MakeRelease("order-service");
        var releaseB = MakeRelease("payment-service");
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[releaseA, releaseB]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetSoc2ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetSoc2ComplianceReportFeature.Query(90, "order-service"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalReleases.Should().Be(1);
        result.Value.ServiceFilter.Should().Be("order-service");
    }

    [Fact]
    public async Task GetSoc2ComplianceReport_Has_Correct_Period()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetSoc2ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetSoc2ComplianceReportFeature.Query(180), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PeriodDays.Should().Be(180);
        result.Value.GeneratedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task GetSoc2ComplianceReport_Validator_Rejects_Days_Out_Of_Range()
    {
        var validator = new GetSoc2ComplianceReportFeature.Validator();

        var validationLow = await validator.ValidateAsync(new GetSoc2ComplianceReportFeature.Query(Days: 0));
        validationLow.IsValid.Should().BeFalse();

        var validationHigh = await validator.ValidateAsync(new GetSoc2ComplianceReportFeature.Query(Days: 366));
        validationHigh.IsValid.Should().BeFalse();
    }
}
