using System.Linq;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeRollbackRecommendation;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

using GetRollbackFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeRollbackRecommendation.GetChangeRollbackRecommendation;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para Wave J.3 — Change Rollback Recommendation.
/// Cobre scoring de urgência de rollback por confidence, blast radius e evidence integrity.
/// </summary>
public sealed class GetChangeRollbackRecommendationTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ApiAssetId = Guid.NewGuid();

    private static Release MakeRelease(string service = "payment-api")
        => Release.Create(TenantId, ApiAssetId, service, "2.0.0", "production", "jenkins", "abc123", FixedNow);

    private static BlastRadiusReport MakeBlastRadius(ReleaseId releaseId, int direct, int transitive = 0)
        => BlastRadiusReport.Calculate(releaseId, ApiAssetId,
            Enumerable.Range(0, direct).Select(i => $"consumer-{i}").ToList(),
            Enumerable.Range(0, transitive).Select(i => $"transitive-{i}").ToList(),
            FixedNow);

    private static ChangeConfidenceBreakdown MakeBreakdown(ReleaseId releaseId, decimal score)
    {
        var subScore = ChangeConfidenceSubScore.Create(
            ConfidenceSubScoreType.TestCoverage, score, 1m,
            ConfidenceDataQuality.High, "test coverage", [], null);
        return ChangeConfidenceBreakdown.Create(releaseId, [subScore], FixedNow);
    }

    private static EvidencePack MakeEvidencePack(Guid releaseId, bool signed = true)
    {
        var pack = EvidencePack.Create(WorkflowInstanceId.New(), releaseId, FixedNow);
        if (signed) pack.ApplyIntegritySignature("{}", "hmac-hash", "auditor@example.com", FixedNow);
        return pack;
    }

    // ── Urgency classification ───────────────────────────────────────────────

    [Fact]
    public async Task GetChangeRollbackRecommendation_Returns_None_When_High_Confidence_And_Low_Blast_Radius()
    {
        var release = MakeRelease();
        var releaseId = release.Id;
        var blast = MakeBlastRadius(releaseId, 2);
        var confidence = MakeBreakdown(releaseId, 85m);
        var pack = MakeEvidencePack(releaseId.Value, signed: true);

        var releaseRepo = Substitute.For<IReleaseRepository>();
        var confRepo = Substitute.For<IChangeConfidenceBreakdownRepository>();
        var blastRepo = Substitute.For<IBlastRadiusRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(release);
        confRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(confidence);
        blastRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(blast);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[pack]);

        var handler = new GetRollbackFeature.Handler(releaseRepo, confRepo, blastRepo, evidenceRepo);
        var result = await handler.Handle(new GetRollbackFeature.Query(release.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Urgency.Should().Be(GetRollbackFeature.RollbackUrgency.None);
        result.Value.RollbackScore.Should().BeLessThan(25);
    }

    [Fact]
    public async Task GetChangeRollbackRecommendation_Returns_Critical_When_Very_Low_Confidence_And_High_Blast_Radius()
    {
        var release = MakeRelease();
        var releaseId = release.Id;
        var blast = MakeBlastRadius(releaseId, 15, 10); // 25 total → high blast radius
        var confidence = MakeBreakdown(releaseId, 20m); // very low confidence → +40 penalty

        var releaseRepo = Substitute.For<IReleaseRepository>();
        var confRepo = Substitute.For<IChangeConfidenceBreakdownRepository>();
        var blastRepo = Substitute.For<IBlastRadiusRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(release);
        confRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(confidence);
        blastRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(blast);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetRollbackFeature.Handler(releaseRepo, confRepo, blastRepo, evidenceRepo);
        var result = await handler.Handle(new GetRollbackFeature.Query(release.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Urgency.Should().Be(GetRollbackFeature.RollbackUrgency.Critical);
        result.Value.RollbackScore.Should().BeGreaterThanOrEqualTo(75);
    }

    [Fact]
    public async Task GetChangeRollbackRecommendation_Returns_NotFound_When_Release_Missing()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var confRepo = Substitute.For<IChangeConfidenceBreakdownRepository>();
        var blastRepo = Substitute.For<IBlastRadiusRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns((Release?)null);

        var handler = new GetRollbackFeature.Handler(releaseRepo, confRepo, blastRepo, evidenceRepo);
        var result = await handler.Handle(new GetRollbackFeature.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetChangeRollbackRecommendation_Has_Three_Factors()
    {
        var release = MakeRelease();
        var releaseId = release.Id;
        var blast = MakeBlastRadius(releaseId, 3);
        var confidence = MakeBreakdown(releaseId, 70m);

        var releaseRepo = Substitute.For<IReleaseRepository>();
        var confRepo = Substitute.For<IChangeConfidenceBreakdownRepository>();
        var blastRepo = Substitute.For<IBlastRadiusRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(release);
        confRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(confidence);
        blastRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(blast);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetRollbackFeature.Handler(releaseRepo, confRepo, blastRepo, evidenceRepo);
        var result = await handler.Handle(new GetRollbackFeature.Query(release.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Factors.Should().HaveCount(3);
        result.Value.Factors.Select(f => f.FactorName).Should().Contain(["ChangeConfidence", "BlastRadius", "EvidenceIntegrity"]);
    }

    [Fact]
    public async Task GetChangeRollbackRecommendation_Uses_Moderate_Penalty_When_No_Confidence_Data()
    {
        var release = MakeRelease();
        var releaseId = release.Id;
        var blast = MakeBlastRadius(releaseId, 1);

        var releaseRepo = Substitute.For<IReleaseRepository>();
        var confRepo = Substitute.For<IChangeConfidenceBreakdownRepository>();
        var blastRepo = Substitute.For<IBlastRadiusRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(release);
        confRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns((ChangeConfidenceBreakdown?)null);
        blastRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(blast);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetRollbackFeature.Handler(releaseRepo, confRepo, blastRepo, evidenceRepo);
        var result = await handler.Handle(new GetRollbackFeature.Query(release.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasConfidenceData.Should().BeFalse();
        var confidenceFactor = result.Value.Factors.First(f => f.FactorName == "ChangeConfidence");
        confidenceFactor.ScorePenalty.Should().Be(15);
    }

    [Fact]
    public async Task GetChangeRollbackRecommendation_Evidence_Penalty_For_Unsigned_Packs()
    {
        var release = MakeRelease();
        var releaseId = release.Id;
        var confidence = MakeBreakdown(releaseId, 80m);
        var packUnsigned = MakeEvidencePack(releaseId.Value, signed: false);

        var releaseRepo = Substitute.For<IReleaseRepository>();
        var confRepo = Substitute.For<IChangeConfidenceBreakdownRepository>();
        var blastRepo = Substitute.For<IBlastRadiusRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(release);
        confRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(confidence);
        blastRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns((BlastRadiusReport?)null);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[packUnsigned]);

        var handler = new GetRollbackFeature.Handler(releaseRepo, confRepo, blastRepo, evidenceRepo);
        var result = await handler.Handle(new GetRollbackFeature.Query(release.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var evidenceFactor = result.Value.Factors.First(f => f.FactorName == "EvidenceIntegrity");
        evidenceFactor.ScorePenalty.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetChangeRollbackRecommendation_No_Evidence_Penalty_When_All_Signed()
    {
        var release = MakeRelease();
        var releaseId = release.Id;
        var confidence = MakeBreakdown(releaseId, 90m);
        var packSigned = MakeEvidencePack(releaseId.Value, signed: true);

        var releaseRepo = Substitute.For<IReleaseRepository>();
        var confRepo = Substitute.For<IChangeConfidenceBreakdownRepository>();
        var blastRepo = Substitute.For<IBlastRadiusRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(release);
        confRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(confidence);
        blastRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns((BlastRadiusReport?)null);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[packSigned]);

        var handler = new GetRollbackFeature.Handler(releaseRepo, confRepo, blastRepo, evidenceRepo);
        var result = await handler.Handle(new GetRollbackFeature.Query(release.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var evidenceFactor = result.Value.Factors.First(f => f.FactorName == "EvidenceIntegrity");
        evidenceFactor.ScorePenalty.Should().Be(0);
    }

    [Fact]
    public async Task GetChangeRollbackRecommendation_Validator_Rejects_Empty_ReleaseId()
    {
        var validator = new GetRollbackFeature.Validator();
        var result = validator.Validate(new GetRollbackFeature.Query(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetChangeRollbackRecommendation_Response_Contains_ServiceName_And_Environment()
    {
        var release = MakeRelease("checkout-service");
        var releaseId = release.Id;
        var confidence = MakeBreakdown(releaseId, 75m);

        var releaseRepo = Substitute.For<IReleaseRepository>();
        var confRepo = Substitute.For<IChangeConfidenceBreakdownRepository>();
        var blastRepo = Substitute.For<IBlastRadiusRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(release);
        confRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(confidence);
        blastRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns((BlastRadiusReport?)null);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetRollbackFeature.Handler(releaseRepo, confRepo, blastRepo, evidenceRepo);
        var result = await handler.Handle(new GetRollbackFeature.Query(release.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("checkout-service");
        result.Value.Environment.Should().Be("production");
        result.Value.ReleaseId.Should().Be(release.Id.Value);
    }

    [Fact]
    public async Task GetChangeRollbackRecommendation_Score_Clamped_To_100()
    {
        var release = MakeRelease();
        var releaseId = release.Id;
        var blast = MakeBlastRadius(releaseId, 25, 25); // very high blast radius
        var confidence = MakeBreakdown(releaseId, 5m); // extremely low confidence

        var releaseRepo = Substitute.For<IReleaseRepository>();
        var confRepo = Substitute.For<IChangeConfidenceBreakdownRepository>();
        var blastRepo = Substitute.For<IBlastRadiusRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(release);
        confRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(confidence);
        blastRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(blast);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetRollbackFeature.Handler(releaseRepo, confRepo, blastRepo, evidenceRepo);
        var result = await handler.Handle(new GetRollbackFeature.Query(release.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RollbackScore.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task GetChangeRollbackRecommendation_Suggest_When_Moderate_Risk()
    {
        var release = MakeRelease();
        var releaseId = release.Id;
        var blast = MakeBlastRadius(releaseId, 3); // moderate blast radius (5 pts)
        var confidence = MakeBreakdown(releaseId, 45m); // low confidence → +25 pts

        var releaseRepo = Substitute.For<IReleaseRepository>();
        var confRepo = Substitute.For<IChangeConfidenceBreakdownRepository>();
        var blastRepo = Substitute.For<IBlastRadiusRepository>();
        var evidenceRepo = Substitute.For<IEvidencePackRepository>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(release);
        confRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(confidence);
        blastRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(blast);
        evidenceRepo.ListByReleaseIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EvidencePack>)[]);

        var handler = new GetRollbackFeature.Handler(releaseRepo, confRepo, blastRepo, evidenceRepo);
        var result = await handler.Handle(new GetRollbackFeature.Query(release.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // 25 (confidence) + 5 (low blast) + 10 (no evidence) = 40 → Suggest
        result.Value.Urgency.Should().Be(GetRollbackFeature.RollbackUrgency.Suggest);
    }
}
