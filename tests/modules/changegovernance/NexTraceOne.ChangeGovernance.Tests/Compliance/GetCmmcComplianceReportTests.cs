using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetCmmcComplianceReport;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

using GetCmmcFeature = NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetCmmcComplianceReport.GetCmmcComplianceReport;

namespace NexTraceOne.ChangeGovernance.Tests.Compliance;

/// <summary>
/// Testes unitários para Wave K.2 — CMMC 2.0 Compliance Report.
/// Cobre avaliação de práticas AC.1.001, IA.1.076, AU.2.041, IR.2.092 e RM.2.141.
/// </summary>
public sealed class GetCmmcComplianceReportTests
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

    private static Release MakeRelease(string serviceName = "cui-system")
        => Release.Create(TenantId, Guid.NewGuid(), serviceName, "1.0.0", "production", "jenkins", "abc123", FixedNow);

    private static EvidencePack MakeEvidencePack(Guid releaseId, bool signed = false)
    {
        var pack = EvidencePack.Create(WorkflowInstanceId.New(), releaseId, FixedNow);
        if (signed) pack.ApplyIntegritySignature("{}", "hmac-hash", "sec@example.com", FixedNow);
        return pack;
    }

    [Fact]
    public async Task GetCmmcComplianceReport_Returns_NotAssessed_When_No_Data()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetCmmcFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetCmmcFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be(Nis2ControlStatus.NotAssessed);
        result.Value.TotalReleases.Should().Be(0);
    }

    [Fact]
    public async Task GetCmmcComplianceReport_Has_Five_Controls()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetCmmcFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetCmmcFeature.Query(30), CancellationToken.None);

        result.Value.Controls.Should().HaveCount(5);
        result.Value.Controls.Select(c => c.PracticeId).Should().Contain(["AC.1.001", "IA.1.076", "AU.2.041", "IR.2.092", "RM.2.141"]);
    }

    [Fact]
    public async Task GetCmmcComplianceReport_AU2_Compliant_When_All_Evidence_Signed()
    {
        var release = MakeRelease();
        var pack = MakeEvidencePack(release.Id.Value, signed: true);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[pack]);

        var handler = new GetCmmcFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetCmmcFeature.Query(90), CancellationToken.None);

        var au2 = result.Value.Controls.First(c => c.PracticeId == "AU.2.041");
        au2.Status.Should().Be(Nis2ControlStatus.Compliant);
    }

    [Fact]
    public async Task GetCmmcComplianceReport_AC1_PartiallyCompliant_When_Releases_Exist()
    {
        var release = MakeRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetCmmcFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetCmmcFeature.Query(90), CancellationToken.None);

        var ac1 = result.Value.Controls.First(c => c.PracticeId == "AC.1.001");
        ac1.Status.Should().Be(Nis2ControlStatus.PartiallyCompliant);
    }

    [Fact]
    public async Task GetCmmcComplianceReport_Overall_PartiallyCompliant_When_Releases_Exist()
    {
        var release = MakeRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetCmmcFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetCmmcFeature.Query(90), CancellationToken.None);

        result.Value.OverallStatus.Should().Be(Nis2ControlStatus.PartiallyCompliant);
    }

    [Fact]
    public async Task GetCmmcComplianceReport_FiltersByServiceName()
    {
        var releaseA = MakeRelease("cui-system");
        var releaseB = MakeRelease("public-api");
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[releaseA, releaseB]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetCmmcFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetCmmcFeature.Query(90, "cui-system"), CancellationToken.None);

        result.Value.TotalReleases.Should().Be(1);
        result.Value.ServiceFilter.Should().Be("cui-system");
    }

    [Fact]
    public async Task GetCmmcComplianceReport_CmmcLevel_Is_2()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetCmmcFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetCmmcFeature.Query(90), CancellationToken.None);

        result.Value.CmmcLevel.Should().Be(2);
    }

    [Fact]
    public async Task GetCmmcComplianceReport_Controls_Have_Domain_Information()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetCmmcFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetCmmcFeature.Query(90), CancellationToken.None);

        result.Value.Controls.All(c => !string.IsNullOrWhiteSpace(c.Domain)).Should().BeTrue();
        result.Value.Controls.All(c => !string.IsNullOrWhiteSpace(c.PracticeName)).Should().BeTrue();
        result.Value.Controls.All(c => !string.IsNullOrWhiteSpace(c.Note)).Should().BeTrue();
    }

    [Fact]
    public void GetCmmcComplianceReport_Validator_Rejects_Invalid_Days()
    {
        var validator = new GetCmmcFeature.Validator();
        var low = validator.Validate(new GetCmmcFeature.Query(0));
        var high = validator.Validate(new GetCmmcFeature.Query(366));
        low.IsValid.Should().BeFalse();
        high.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetCmmcComplianceReport_Validator_Accepts_Valid_Days()
    {
        var validator = new GetCmmcFeature.Validator();
        var result = validator.Validate(new GetCmmcFeature.Query(90));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GetCmmcComplianceReport_SignedEvidencePacks_Counted_Correctly()
    {
        var release = MakeRelease();
        var signed = MakeEvidencePack(release.Id.Value, signed: true);
        var unsigned = MakeEvidencePack(release.Id.Value, signed: false);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[signed, unsigned]);

        var handler = new GetCmmcFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetCmmcFeature.Query(90), CancellationToken.None);

        result.Value.SignedEvidencePacks.Should().Be(1);
        result.Value.TotalEvidencePacks.Should().Be(2);
    }
}
