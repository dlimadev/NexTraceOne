using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetIso27001ComplianceReport;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

using GetIso27001ComplianceReportFeature = NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetIso27001ComplianceReport.GetIso27001ComplianceReport;

namespace NexTraceOne.ChangeGovernance.Tests.Compliance;

/// <summary>
/// Testes unitários para Wave G.2 — ISO/IEC 27001:2022 Compliance Report.
/// Cobre avaliação de controlos A.8.8, A.8.32, A.5.26, A.5.29 e A.8.9.
/// </summary>
public sealed class GetIso27001ComplianceReportTests
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
    public async Task GetIso27001ComplianceReport_Returns_NotAssessed_When_No_Data()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetIso27001ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetIso27001ComplianceReportFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be(Nis2ControlStatus.NotAssessed);
    }

    [Fact]
    public async Task GetIso27001ComplianceReport_A832_Compliant_When_All_Evidence_Signed()
    {
        var release = MakeRelease();
        var pack = MakeEvidencePack(release.Id.Value, signed: true);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[pack]);

        var handler = new GetIso27001ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetIso27001ComplianceReportFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var a832 = result.Value.Controls.Single(c => c.ControlId == "A.8.32");
        a832.Status.Should().Be(Nis2ControlStatus.Compliant);
    }

    [Fact]
    public async Task GetIso27001ComplianceReport_A832_NonCompliant_When_No_Evidence_Signed()
    {
        var release = MakeRelease();
        var pack = MakeEvidencePack(release.Id.Value, signed: false);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[pack]);

        var handler = new GetIso27001ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetIso27001ComplianceReportFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var a832 = result.Value.Controls.Single(c => c.ControlId == "A.8.32");
        a832.Status.Should().Be(Nis2ControlStatus.NonCompliant);
    }

    [Fact]
    public async Task GetIso27001ComplianceReport_A88_And_A526_Are_NotAssessed()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetIso27001ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetIso27001ComplianceReportFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Controls.Single(c => c.ControlId == "A.8.8").Status.Should().Be(Nis2ControlStatus.NotAssessed);
        result.Value.Controls.Single(c => c.ControlId == "A.5.26").Status.Should().Be(Nis2ControlStatus.NotAssessed);
    }

    [Fact]
    public async Task GetIso27001ComplianceReport_Returns_All_Five_Controls()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetIso27001ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetIso27001ComplianceReportFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Controls.Should().HaveCount(5);
        result.Value.Controls.Select(c => c.ControlId)
            .Should().BeEquivalentTo(["A.8.8", "A.8.32", "A.5.26", "A.5.29", "A.8.9"]);
    }

    [Fact]
    public async Task GetIso27001ComplianceReport_A89_Compliant_When_Releases_Exist()
    {
        var release = MakeRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetIso27001ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetIso27001ComplianceReportFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var a89 = result.Value.Controls.Single(c => c.ControlId == "A.8.9");
        a89.Status.Should().Be(Nis2ControlStatus.Compliant);
    }

    [Fact]
    public async Task GetIso27001ComplianceReport_Filters_By_ServiceName()
    {
        var releaseA = MakeRelease("order-service");
        var releaseB = MakeRelease("payment-service");
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[releaseA, releaseB]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetIso27001ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetIso27001ComplianceReportFeature.Query(90, "order-service"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalReleases.Should().Be(1);
        result.Value.ServiceFilter.Should().Be("order-service");
    }

    [Fact]
    public async Task GetIso27001ComplianceReport_Validator_Rejects_Invalid_Period()
    {
        var validator = new GetIso27001ComplianceReportFeature.Validator();

        var invalid = await validator.ValidateAsync(new GetIso27001ComplianceReportFeature.Query(Days: 0));
        invalid.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetIso27001ComplianceReport_Has_Correct_Period_And_GeneratedAt()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetIso27001ComplianceReportFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetIso27001ComplianceReportFeature.Query(120), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PeriodDays.Should().Be(120);
        result.Value.GeneratedAt.Should().Be(FixedNow);
    }
}
