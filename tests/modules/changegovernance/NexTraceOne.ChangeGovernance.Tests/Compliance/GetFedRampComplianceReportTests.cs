using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetFedRampComplianceReport;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

using GetFedRampFeature = NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetFedRampComplianceReport.GetFedRampComplianceReport;

namespace NexTraceOne.ChangeGovernance.Tests.Compliance;

/// <summary>
/// Testes unitários para Wave L.2 — FedRAMP Moderate Compliance Report.
/// Cobre avaliação de controlos AC-2, AU-2, CM-6, IR-4 e SI-2.
/// </summary>
public sealed class GetFedRampComplianceReportTests
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

    private static Release MakeRelease(string serviceName = "fedramp-system")
        => Release.Create(TenantId, Guid.NewGuid(), serviceName, "1.0.0", "production", "jenkins", "abc123", FixedNow);

    private static EvidencePack MakeEvidencePack(Guid releaseId, bool signed = false)
    {
        var pack = EvidencePack.Create(WorkflowInstanceId.New(), releaseId, FixedNow);
        if (signed) pack.ApplyIntegritySignature("{}", "hmac-hash", "sec@example.com", FixedNow);
        return pack;
    }

    [Fact]
    public async Task GetFedRampComplianceReport_Returns_NotAssessed_When_No_Data()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetFedRampFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetFedRampFeature.Query(90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be(Nis2ControlStatus.NotAssessed);
        result.Value.TotalReleases.Should().Be(0);
    }

    [Fact]
    public async Task GetFedRampComplianceReport_Has_Five_Controls()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetFedRampFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetFedRampFeature.Query(30), CancellationToken.None);

        result.Value.Controls.Should().HaveCount(5);
        result.Value.Controls.Select(c => c.ControlId).Should().Contain(["AC-2", "AU-2", "CM-6", "IR-4", "SI-2"]);
    }

    [Fact]
    public async Task GetFedRampComplianceReport_AU2_Compliant_When_All_Packs_Signed()
    {
        var release = MakeRelease();
        var pack = MakeEvidencePack(release.Id.Value, signed: true);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[pack]);

        var handler = new GetFedRampFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetFedRampFeature.Query(90), CancellationToken.None);

        var au2 = result.Value.Controls.First(c => c.ControlId == "AU-2");
        au2.Status.Should().Be(Nis2ControlStatus.Compliant);
    }

    [Fact]
    public async Task GetFedRampComplianceReport_AU2_PartiallyCompliant_When_Some_Packs_Unsigned()
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

        var handler = new GetFedRampFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetFedRampFeature.Query(90), CancellationToken.None);

        var au2 = result.Value.Controls.First(c => c.ControlId == "AU-2");
        au2.Status.Should().Be(Nis2ControlStatus.PartiallyCompliant);
    }

    [Fact]
    public async Task GetFedRampComplianceReport_Overall_PartiallyCompliant_When_Releases_Exist()
    {
        var release = MakeRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetFedRampFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetFedRampFeature.Query(90), CancellationToken.None);

        result.Value.OverallStatus.Should().Be(Nis2ControlStatus.PartiallyCompliant);
    }

    [Fact]
    public async Task GetFedRampComplianceReport_ImpactLevel_Is_Moderate()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetFedRampFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetFedRampFeature.Query(90), CancellationToken.None);

        result.Value.ImpactLevel.Should().Be("Moderate");
    }

    [Fact]
    public async Task GetFedRampComplianceReport_FiltersByServiceName()
    {
        var releaseA = MakeRelease("fedramp-system");
        var releaseB = MakeRelease("public-api");
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[releaseA, releaseB]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetFedRampFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetFedRampFeature.Query(90, "fedramp-system"), CancellationToken.None);

        result.Value.TotalReleases.Should().Be(1);
        result.Value.ServiceFilter.Should().Be("fedramp-system");
    }

    [Fact]
    public async Task GetFedRampComplianceReport_Controls_Have_ControlFamily_And_Note()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetFedRampFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetFedRampFeature.Query(90), CancellationToken.None);

        result.Value.Controls.All(c => !string.IsNullOrWhiteSpace(c.ControlFamily)).Should().BeTrue();
        result.Value.Controls.All(c => !string.IsNullOrWhiteSpace(c.ControlName)).Should().BeTrue();
        result.Value.Controls.All(c => !string.IsNullOrWhiteSpace(c.Note)).Should().BeTrue();
    }

    [Fact]
    public async Task GetFedRampComplianceReport_SignedEvidencePacks_CountedCorrectly()
    {
        var release = MakeRelease();
        var s1 = MakeEvidencePack(release.Id.Value, signed: true);
        var s2 = MakeEvidencePack(release.Id.Value, signed: true);
        var u1 = MakeEvidencePack(release.Id.Value, signed: false);
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();
        releaseRepo.ListInRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Release>)[release]);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[s1, s2, u1]);

        var handler = new GetFedRampFeature.Handler(releaseRepo, evidenceRepo, CreateTenant(), CreateClock());
        var result = await handler.Handle(new GetFedRampFeature.Query(90), CancellationToken.None);

        result.Value.SignedEvidencePacks.Should().Be(2);
        result.Value.TotalEvidencePacks.Should().Be(3);
    }

    [Fact]
    public void GetFedRampComplianceReport_Validator_Rejects_InvalidDays()
    {
        var validator = new GetFedRampFeature.Validator();
        validator.Validate(new GetFedRampFeature.Query(0)).IsValid.Should().BeFalse();
        validator.Validate(new GetFedRampFeature.Query(366)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetFedRampComplianceReport_Validator_Accepts_ValidQuery()
    {
        var validator = new GetFedRampFeature.Validator();
        validator.Validate(new GetFedRampFeature.Query(90)).IsValid.Should().BeTrue();
        validator.Validate(new GetFedRampFeature.Query(90, "api-system")).IsValid.Should().BeTrue();
    }
}
