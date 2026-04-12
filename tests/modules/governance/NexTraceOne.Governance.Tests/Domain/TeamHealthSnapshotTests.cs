using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Tests.Domain;

/// <summary>
/// Testes unitários para a entidade TeamHealthSnapshot.
/// Valida cálculo de OverallScore, recomputação, guard clauses e limites de score.
/// </summary>
public sealed class TeamHealthSnapshotTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);

    // ── Factory method: Compute ──

    [Fact]
    public void Compute_AllScores100_ShouldReturnOverall100()
    {
        var snapshot = CreateSnapshot(100, 100, 100, 100, 100, 100, 100);

        snapshot.OverallScore.Should().Be(100);
    }

    [Fact]
    public void Compute_AllScores0_ShouldReturnOverall0()
    {
        var snapshot = CreateSnapshot(0, 0, 0, 0, 0, 0, 0);

        snapshot.OverallScore.Should().Be(0);
    }

    [Fact]
    public void Compute_MixedScores_ShouldReturnRoundedAverage()
    {
        // (80 + 60 + 70 + 50 + 40 + 90 + 75) = 465 / 7 = 66.4286 → rounds to 66
        var snapshot = CreateSnapshot(80, 60, 70, 50, 40, 90, 75);

        snapshot.OverallScore.Should().Be(66);
    }

    [Fact]
    public void Compute_ScoresThatRoundUp_ShouldRoundCorrectly()
    {
        // (100 + 100 + 100 + 100 + 100 + 100 + 50) = 650 / 7 = 92.857 → rounds to 93
        var snapshot = CreateSnapshot(100, 100, 100, 100, 100, 100, 50);

        snapshot.OverallScore.Should().Be(93);
    }

    [Fact]
    public void Compute_ShouldSetAllPropertiesCorrectly()
    {
        var teamId = Guid.NewGuid();
        var snapshot = TeamHealthSnapshot.Compute(
            teamId: teamId,
            teamName: "  platform-team  ",
            serviceCountScore: 85,
            contractHealthScore: 70,
            incidentFrequencyScore: 60,
            mttrScore: 55,
            techDebtScore: 40,
            docCoverageScore: 90,
            policyComplianceScore: 75,
            dimensionDetails: """{"notes":"test"}""",
            tenantId: "  tenant-abc  ",
            now: FixedNow);

        snapshot.TeamId.Should().Be(teamId);
        snapshot.TeamName.Should().Be("platform-team");
        snapshot.ServiceCountScore.Should().Be(85);
        snapshot.ContractHealthScore.Should().Be(70);
        snapshot.IncidentFrequencyScore.Should().Be(60);
        snapshot.MttrScore.Should().Be(55);
        snapshot.TechDebtScore.Should().Be(40);
        snapshot.DocCoverageScore.Should().Be(90);
        snapshot.PolicyComplianceScore.Should().Be(75);
        snapshot.DimensionDetails.Should().Be("""{"notes":"test"}""");
        snapshot.TenantId.Should().Be("tenant-abc");
        snapshot.AssessedAt.Should().Be(FixedNow);
        snapshot.Id.Value.Should().NotBe(Guid.Empty);
    }

    // ── Recompute ──

    [Fact]
    public void Recompute_ShouldUpdateScoresAndOverall()
    {
        var snapshot = CreateSnapshot(50, 50, 50, 50, 50, 50, 50);
        snapshot.OverallScore.Should().Be(50);

        var laterNow = FixedNow.AddDays(7);
        snapshot.Recompute(
            serviceCountScore: 80,
            contractHealthScore: 90,
            incidentFrequencyScore: 70,
            mttrScore: 60,
            techDebtScore: 85,
            docCoverageScore: 95,
            policyComplianceScore: 75,
            dimensionDetails: null,
            now: laterNow);

        // (80 + 90 + 70 + 60 + 85 + 95 + 75) = 555 / 7 = 79.2857 → rounds to 79
        snapshot.OverallScore.Should().Be(79);
        snapshot.ServiceCountScore.Should().Be(80);
        snapshot.ContractHealthScore.Should().Be(90);
        snapshot.AssessedAt.Should().Be(laterNow);
    }

    [Fact]
    public void Recompute_ShouldUpdateDimensionDetails()
    {
        var snapshot = CreateSnapshot(50, 50, 50, 50, 50, 50, 50);
        snapshot.DimensionDetails.Should().BeNull();

        snapshot.Recompute(50, 50, 50, 50, 50, 50, 50, """{"updated":true}""", FixedNow.AddDays(1));

        snapshot.DimensionDetails.Should().Be("""{"updated":true}""");
    }

    // ── Guard clauses ──

    [Fact]
    public void Compute_DefaultTeamId_ShouldThrow()
    {
        var act = () => TeamHealthSnapshot.Compute(
            teamId: Guid.Empty,
            teamName: "team",
            serviceCountScore: 50,
            contractHealthScore: 50,
            incidentFrequencyScore: 50,
            mttrScore: 50,
            techDebtScore: 50,
            docCoverageScore: 50,
            policyComplianceScore: 50,
            dimensionDetails: null,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Compute_NullTeamName_ShouldThrow()
    {
        var act = () => TeamHealthSnapshot.Compute(
            teamId: Guid.NewGuid(),
            teamName: null!,
            serviceCountScore: 50,
            contractHealthScore: 50,
            incidentFrequencyScore: 50,
            mttrScore: 50,
            techDebtScore: 50,
            docCoverageScore: 50,
            policyComplianceScore: 50,
            dimensionDetails: null,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Compute_EmptyTeamName_ShouldThrow()
    {
        var act = () => TeamHealthSnapshot.Compute(
            teamId: Guid.NewGuid(),
            teamName: "   ",
            serviceCountScore: 50,
            contractHealthScore: 50,
            incidentFrequencyScore: 50,
            mttrScore: 50,
            techDebtScore: 50,
            docCoverageScore: 50,
            policyComplianceScore: 50,
            dimensionDetails: null,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Compute_TeamNameTooLong_ShouldThrow()
    {
        var act = () => TeamHealthSnapshot.Compute(
            teamId: Guid.NewGuid(),
            teamName: new string('A', 201),
            serviceCountScore: 50,
            contractHealthScore: 50,
            incidentFrequencyScore: 50,
            mttrScore: 50,
            techDebtScore: 50,
            docCoverageScore: 50,
            policyComplianceScore: 50,
            dimensionDetails: null,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Compute_NegativeScore_ShouldThrow()
    {
        var act = () => TeamHealthSnapshot.Compute(
            teamId: Guid.NewGuid(),
            teamName: "team",
            serviceCountScore: -1,
            contractHealthScore: 50,
            incidentFrequencyScore: 50,
            mttrScore: 50,
            techDebtScore: 50,
            docCoverageScore: 50,
            policyComplianceScore: 50,
            dimensionDetails: null,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Compute_ScoreAbove100_ShouldThrow()
    {
        var act = () => TeamHealthSnapshot.Compute(
            teamId: Guid.NewGuid(),
            teamName: "team",
            serviceCountScore: 50,
            contractHealthScore: 101,
            incidentFrequencyScore: 50,
            mttrScore: 50,
            techDebtScore: 50,
            docCoverageScore: 50,
            policyComplianceScore: 50,
            dimensionDetails: null,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Recompute_NegativeScore_ShouldThrow()
    {
        var snapshot = CreateSnapshot(50, 50, 50, 50, 50, 50, 50);

        var act = () => snapshot.Recompute(-1, 50, 50, 50, 50, 50, 50, null, FixedNow.AddDays(1));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Recompute_ScoreAbove100_ShouldThrow()
    {
        var snapshot = CreateSnapshot(50, 50, 50, 50, 50, 50, 50);

        var act = () => snapshot.Recompute(50, 50, 50, 50, 50, 50, 101, null, FixedNow.AddDays(1));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── Helper ──

    private static TeamHealthSnapshot CreateSnapshot(
        int sc, int ch, int inf, int mttr, int td, int dc, int pc) =>
        TeamHealthSnapshot.Compute(
            teamId: Guid.NewGuid(),
            teamName: "test-team",
            serviceCountScore: sc,
            contractHealthScore: ch,
            incidentFrequencyScore: inf,
            mttrScore: mttr,
            techDebtScore: td,
            docCoverageScore: dc,
            policyComplianceScore: pc,
            dimensionDetails: null,
            tenantId: null,
            now: FixedNow);
}
