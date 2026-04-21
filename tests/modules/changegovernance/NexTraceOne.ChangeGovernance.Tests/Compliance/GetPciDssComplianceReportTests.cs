using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetPciDssComplianceReport;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

using GetPciDssFeature = NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetPciDssComplianceReport.GetPciDssComplianceReport;

namespace NexTraceOne.ChangeGovernance.Tests.Compliance;

/// <summary>
/// Testes unitários para Wave H.2 — PCI-DSS Compliance Report.
/// Cobre avaliação de requisitos Req 1-2, Req 6, Req 10, Req 11 e Req 12, e estado global.
/// </summary>
public sealed class GetPciDssComplianceReportTests
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

    private static Release MakeRelease(string serviceName = "payment-service")
        => Release.Create(TenantId, Guid.NewGuid(), serviceName, "1.0.0", "production", "github-actions", "abc123", FixedNow);

    private static EvidencePack MakeEvidencePack(Guid releaseId, bool signed = false)
    {
        var pack = EvidencePack.Create(WorkflowInstanceId.New(), releaseId, FixedNow);
        if (signed) pack.ApplyIntegritySignature("{}", "hash123", "auditor@test.com", FixedNow);
        return pack;
    }

    // ── Core compliance tests ────────────────────────────────────────────────

    [Fact]
    public async Task GetPciDssComplianceReport_Returns_NotAssessed_When_No_Data()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetPciDssFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetPciDssFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be(Nis2ControlStatus.NotAssessed);
    }

    [Fact]
    public async Task GetPciDssComplianceReport_Req6_Compliant_When_All_Evidence_Signed()
    {
        var release = MakeRelease();
        var pack = MakeEvidencePack(release.Id.Value, signed: true);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[pack]);

        var handler = new GetPciDssFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetPciDssFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var req6 = result.Value.Controls.Single(c => c.RequirementId == "Req 6");
        req6.Status.Should().Be(Nis2ControlStatus.Compliant);
    }

    [Fact]
    public async Task GetPciDssComplianceReport_Req6_NonCompliant_When_No_Evidence_Signed()
    {
        var release = MakeRelease();
        var packUnsigned = MakeEvidencePack(release.Id.Value, signed: false);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[packUnsigned]);

        var handler = new GetPciDssFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetPciDssFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var req6 = result.Value.Controls.Single(c => c.RequirementId == "Req 6");
        req6.Status.Should().Be(Nis2ControlStatus.NonCompliant);
    }

    [Fact]
    public async Task GetPciDssComplianceReport_Req6_PartiallyCompliant_When_Some_Evidence_Signed()
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

        var handler = new GetPciDssFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetPciDssFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var req6 = result.Value.Controls.Single(c => c.RequirementId == "Req 6");
        req6.Status.Should().Be(Nis2ControlStatus.PartiallyCompliant);
    }

    [Fact]
    public async Task GetPciDssComplianceReport_Req10_Compliant_When_Releases_Exist()
    {
        var release = MakeRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetPciDssFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetPciDssFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var req10 = result.Value.Controls.Single(c => c.RequirementId == "Req 10");
        req10.Status.Should().Be(Nis2ControlStatus.Compliant);
    }

    [Fact]
    public async Task GetPciDssComplianceReport_Req12_And_Req11_Are_NotAssessed_Or_Partial()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetPciDssFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetPciDssFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var req12 = result.Value.Controls.Single(c => c.RequirementId == "Req 1-2");
        req12.Status.Should().Be(Nis2ControlStatus.NotAssessed);
        var req11 = result.Value.Controls.Single(c => c.RequirementId == "Req 11");
        req11.Status.Should().Be(Nis2ControlStatus.NotAssessed);
    }

    [Fact]
    public async Task GetPciDssComplianceReport_Returns_All_Five_Controls()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetPciDssFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetPciDssFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Controls.Should().HaveCount(5);
        result.Value.Controls.Select(c => c.RequirementId)
            .Should().BeEquivalentTo(["Req 1-2", "Req 6", "Req 10", "Req 11", "Req 12"]);
    }

    [Fact]
    public async Task GetPciDssComplianceReport_Overall_NonCompliant_When_Any_NonCompliant()
    {
        var release = MakeRelease();
        var packUnsigned = MakeEvidencePack(release.Id.Value, signed: false);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[packUnsigned]);

        var handler = new GetPciDssFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetPciDssFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be(Nis2ControlStatus.NonCompliant);
    }

    [Fact]
    public async Task GetPciDssComplianceReport_Filters_By_ServiceName()
    {
        var releaseA = MakeRelease("payment-service");
        var releaseB = MakeRelease("order-service");
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[releaseA, releaseB]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetPciDssFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetPciDssFeature.Query(90, "payment-service"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalReleases.Should().Be(1);
        result.Value.ServiceFilter.Should().Be("payment-service");
    }

    [Fact]
    public async Task GetPciDssComplianceReport_Validator_Rejects_Days_Out_Of_Range()
    {
        var validator = new GetPciDssFeature.Validator();

        var resultLow = await validator.ValidateAsync(new GetPciDssFeature.Query(Days: 0));
        resultLow.IsValid.Should().BeFalse();

        var resultHigh = await validator.ValidateAsync(new GetPciDssFeature.Query(Days: 366));
        resultHigh.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetPciDssComplianceReport_Has_Correct_Period_And_Timestamp()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetPciDssFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetPciDssFeature.Query(180), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PeriodDays.Should().Be(180);
        result.Value.GeneratedAt.Should().Be(FixedNow);
    }
}
