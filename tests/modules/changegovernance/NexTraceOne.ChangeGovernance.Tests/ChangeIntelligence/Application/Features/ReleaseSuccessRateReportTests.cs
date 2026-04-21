using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetReleaseSuccessRateReport;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

using SuccessRateTier = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetReleaseSuccessRateReport.GetReleaseSuccessRateReport.SuccessRateTier;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para Wave P.3 — GetReleaseSuccessRateReport.
/// Cobre: relatório vazio, distribuição por status, distribuição por ambiente,
/// top serviços por taxa de falha, tier global de sucesso, filtragem por ambiente e validação.
/// </summary>
public sealed class ReleaseSuccessRateReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly string TenantIdStr = TenantId.ToString();

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static Release MakeRelease(
        string serviceName,
        DeploymentStatus status,
        string environment = "production")
    {
        var r = Release.Create(TenantId, Guid.NewGuid(), serviceName, "1.0.0",
            environment, "jenkins", "abc", FixedNow.AddDays(-10));

        // Advance status through valid transitions
        if (status is DeploymentStatus.Running or DeploymentStatus.Succeeded
            or DeploymentStatus.Failed or DeploymentStatus.RolledBack)
            r.UpdateStatus(DeploymentStatus.Running);

        if (status == DeploymentStatus.Succeeded)
            r.UpdateStatus(DeploymentStatus.Succeeded);
        else if (status == DeploymentStatus.Failed)
            r.UpdateStatus(DeploymentStatus.Failed);
        else if (status == DeploymentStatus.RolledBack)
        {
            r.UpdateStatus(DeploymentStatus.Succeeded);
            r.UpdateStatus(DeploymentStatus.RolledBack);
        }
        return r;
    }

    private static GetReleaseSuccessRateReport.Handler CreateHandler(
        IReadOnlyList<Release> releases)
    {
        var repo = Substitute.For<IReleaseRepository>();
        repo.ListInRangeAsync(
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(),
            TenantId,
            Arg.Any<CancellationToken>())
            .Returns(releases);

        return new GetReleaseSuccessRateReport.Handler(repo, CreateClock());
    }

    // ── Empty report ──────────────────────────────────────────────────────

    [Fact]
    public async Task Report_Empty_When_No_Releases()
    {
        var handler = CreateHandler([]);
        var result = await handler.Handle(
            new GetReleaseSuccessRateReport.Query(TenantIdStr), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalReleases.Should().Be(0);
        result.Value.TotalServicesWithReleases.Should().Be(0);
        result.Value.GlobalSuccessRatePercent.Should().Be(0m);
        result.Value.ByStatus.SucceededCount.Should().Be(0);
        result.Value.ByEnvironment.Should().BeEmpty();
        result.Value.TopServicesByFailureRate.Should().BeEmpty();
    }

    // ── Global success rates ──────────────────────────────────────────────

    [Fact]
    public async Task GlobalSuccessRate_Computed_Correctly()
    {
        var releases = new[]
        {
            MakeRelease("svc-a", DeploymentStatus.Succeeded),
            MakeRelease("svc-b", DeploymentStatus.Succeeded),
            MakeRelease("svc-c", DeploymentStatus.Failed),
            MakeRelease("svc-d", DeploymentStatus.RolledBack),
        };
        var handler = CreateHandler(releases);
        var result = await handler.Handle(
            new GetReleaseSuccessRateReport.Query(TenantIdStr), CancellationToken.None);

        result.Value.TotalReleases.Should().Be(4);
        result.Value.GlobalSuccessRatePercent.Should().Be(50m);
        result.Value.GlobalFailureRatePercent.Should().Be(25m);
        result.Value.GlobalRollbackRatePercent.Should().Be(25m);
    }

    // ── Status distribution ───────────────────────────────────────────────

    [Fact]
    public async Task ByStatus_Counts_All_Statuses()
    {
        var releases = new[]
        {
            MakeRelease("svc-a", DeploymentStatus.Pending),
            MakeRelease("svc-b", DeploymentStatus.Running),
            MakeRelease("svc-c", DeploymentStatus.Succeeded),
            MakeRelease("svc-d", DeploymentStatus.Failed),
            MakeRelease("svc-e", DeploymentStatus.RolledBack),
        };
        var handler = CreateHandler(releases);
        var result = await handler.Handle(
            new GetReleaseSuccessRateReport.Query(TenantIdStr), CancellationToken.None);

        result.Value.ByStatus.PendingCount.Should().Be(1);
        result.Value.ByStatus.RunningCount.Should().Be(1);
        result.Value.ByStatus.SucceededCount.Should().Be(1);
        result.Value.ByStatus.FailedCount.Should().Be(1);
        result.Value.ByStatus.RolledBackCount.Should().Be(1);
    }

    // ── Environment distribution ──────────────────────────────────────────

    [Fact]
    public async Task ByEnvironment_Groups_Correctly()
    {
        var releases = new[]
        {
            MakeRelease("svc-a", DeploymentStatus.Succeeded, "production"),
            MakeRelease("svc-b", DeploymentStatus.Failed, "production"),
            MakeRelease("svc-c", DeploymentStatus.Succeeded, "staging"),
        };
        var handler = CreateHandler(releases);
        var result = await handler.Handle(
            new GetReleaseSuccessRateReport.Query(TenantIdStr), CancellationToken.None);

        result.Value.ByEnvironment.Should().HaveCount(2);
        var prod = result.Value.ByEnvironment.First(e => e.Environment == "production");
        prod.TotalReleases.Should().Be(2);
        prod.SucceededCount.Should().Be(1);
        prod.FailedCount.Should().Be(1);
        prod.SuccessRatePercent.Should().Be(50m);
    }

    // ── Top services by failure rate ──────────────────────────────────────

    [Fact]
    public async Task TopServicesByFailureRate_Ordered_Descending_By_FailureRate()
    {
        var releases = new[]
        {
            MakeRelease("svc-perfect", DeploymentStatus.Succeeded),
            MakeRelease("svc-bad", DeploymentStatus.Failed),
            MakeRelease("svc-bad", DeploymentStatus.Failed),
            MakeRelease("svc-medium", DeploymentStatus.Failed),
            MakeRelease("svc-medium", DeploymentStatus.Succeeded),
        };
        var handler = CreateHandler(releases);
        var result = await handler.Handle(
            new GetReleaseSuccessRateReport.Query(TenantIdStr, MaxServices: 10), CancellationToken.None);

        result.Value.TopServicesByFailureRate[0].ServiceName.Should().Be("svc-bad");
        result.Value.TopServicesByFailureRate[0].FailureRatePercent.Should().Be(100m);
    }

    [Fact]
    public async Task TopServicesByFailureRate_Limited_By_MaxServices()
    {
        var releases = Enumerable.Range(1, 10)
            .Select(i => MakeRelease($"svc-{i}", DeploymentStatus.Failed))
            .ToList();

        var handler = CreateHandler(releases);
        var result = await handler.Handle(
            new GetReleaseSuccessRateReport.Query(TenantIdStr, MaxServices: 3), CancellationToken.None);

        result.Value.TopServicesByFailureRate.Should().HaveCount(3);
    }

    // ── SuccessRateTier ───────────────────────────────────────────────────

    [Theory]
    [InlineData(99, SuccessRateTier.Elite)]
    [InlineData(100, SuccessRateTier.Elite)]
    [InlineData(95, SuccessRateTier.High)]
    [InlineData(80, SuccessRateTier.Medium)]
    [InlineData(79, SuccessRateTier.Low)]
    public async Task GlobalSuccessRateTier_Classified_Correctly(int successRatePct, SuccessRateTier expectedTier)
    {
        // Create successRatePct% succeeded releases out of 100
        var releases = Enumerable.Range(1, 100)
            .Select(i => MakeRelease(
                "svc-a",
                i <= successRatePct ? DeploymentStatus.Succeeded : DeploymentStatus.Failed))
            .ToList();

        var handler = CreateHandler(releases);
        var result = await handler.Handle(
            new GetReleaseSuccessRateReport.Query(TenantIdStr), CancellationToken.None);

        result.Value.GlobalSuccessRateTier.Should().Be(expectedTier);
    }

    // ── Per-service success rate entry ────────────────────────────────────

    [Fact]
    public async Task ServiceEntry_ComputesAllRates_Correctly()
    {
        var releases = new[]
        {
            MakeRelease("svc-a", DeploymentStatus.Succeeded),
            MakeRelease("svc-a", DeploymentStatus.Failed),
            MakeRelease("svc-a", DeploymentStatus.RolledBack),
            MakeRelease("svc-a", DeploymentStatus.Succeeded),
        };
        var handler = CreateHandler(releases);
        var result = await handler.Handle(
            new GetReleaseSuccessRateReport.Query(TenantIdStr), CancellationToken.None);

        var entry = result.Value.TopServicesByFailureRate.First(e => e.ServiceName == "svc-a");
        entry.TotalReleases.Should().Be(4);
        entry.SucceededCount.Should().Be(2);
        entry.FailedCount.Should().Be(1);
        entry.RolledBackCount.Should().Be(1);
        entry.SuccessRatePercent.Should().Be(50m);
        entry.FailureRatePercent.Should().Be(25m);
        entry.RollbackRatePercent.Should().Be(25m);
    }

    // ── Report metadata ───────────────────────────────────────────────────

    [Fact]
    public async Task Report_From_To_ComputedFromLookback()
    {
        var handler = CreateHandler([]);
        var result = await handler.Handle(
            new GetReleaseSuccessRateReport.Query(TenantIdStr, LookbackDays: 90), CancellationToken.None);

        result.Value.To.Should().Be(FixedNow);
        result.Value.From.Should().Be(FixedNow.AddDays(-90));
        result.Value.LookbackDays.Should().Be(90);
        result.Value.GeneratedAt.Should().Be(FixedNow);
    }

    // ── Validation ────────────────────────────────────────────────────────

    [Fact]
    public void Validator_Rejects_Empty_TenantId()
    {
        var validator = new GetReleaseSuccessRateReport.Validator();
        var result = validator.Validate(new GetReleaseSuccessRateReport.Query("", 90, null, 20));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Handler_Returns_Failure_For_Invalid_TenantId_Guid_Format()
    {
        var handler = CreateHandler([]);
        var result = await handler.Handle(
            new GetReleaseSuccessRateReport.Query("not-a-guid"), CancellationToken.None);
        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public void Validator_Rejects_Invalid_LookbackDays(int lookbackDays)
    {
        var validator = new GetReleaseSuccessRateReport.Validator();
        var result = validator.Validate(new GetReleaseSuccessRateReport.Query(TenantIdStr, lookbackDays));
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public void Validator_Rejects_Invalid_MaxServices(int maxServices)
    {
        var validator = new GetReleaseSuccessRateReport.Validator();
        var result = validator.Validate(new GetReleaseSuccessRateReport.Query(TenantIdStr, 90, null, maxServices));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Accepts_Valid_Query()
    {
        var validator = new GetReleaseSuccessRateReport.Validator();
        var result = validator.Validate(new GetReleaseSuccessRateReport.Query(TenantIdStr, 90, null, 20));
        result.IsValid.Should().BeTrue();
    }
}
