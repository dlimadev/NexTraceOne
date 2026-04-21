using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetCrossRankedBenchmark;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.RecordBenchmarkConsent;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.SubmitBenchmarkSnapshot;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Entities;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Tests.Compliance;

/// <summary>
/// Testes unitários para os backlog items Wave D.2 — Cross-tenant Benchmarks anonimizados.
/// Cobre: consentimento LGPD, snapshots DORA, percentil cross-tenant e privacidade de dados.
/// </summary>
public sealed class BenchmarkTests
{
    private readonly ITenantBenchmarkConsentRepository _consentRepo = Substitute.For<ITenantBenchmarkConsentRepository>();
    private readonly IBenchmarkSnapshotRepository _snapshotRepo = Substitute.For<IBenchmarkSnapshotRepository>();
    private readonly IChangeIntelligenceUnitOfWork _uow = Substitute.For<IChangeIntelligenceUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private static readonly DateTimeOffset Now = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);

    public BenchmarkTests()
    {
        _clock.UtcNow.Returns(Now);
        _uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    // ── Domain entity tests ───────────────────────────────────────────────

    [Fact]
    public void TenantBenchmarkConsent_RequestConsent_Creates_Pending_Consent()
    {
        var consent = TenantBenchmarkConsent.RequestConsent("tenant-1", "Art. 7, VI LGPD");

        consent.TenantId.Should().Be("tenant-1");
        consent.Status.Should().Be(BenchmarkConsentStatus.Pending);
        consent.IsOptedIn.Should().BeFalse();
        consent.LgpdLawfulBasis.Should().Be("Art. 7, VI LGPD");
    }

    [Fact]
    public void TenantBenchmarkConsent_Grant_Sets_Status_Granted()
    {
        var consent = TenantBenchmarkConsent.RequestConsent("tenant-1", "Art. 7, VI LGPD");
        consent.Grant("admin-user", Now);

        consent.Status.Should().Be(BenchmarkConsentStatus.Granted);
        consent.ConsentedAt.Should().Be(Now);
        consent.ConsentedByUserId.Should().Be("admin-user");
    }

    [Fact]
    public void TenantBenchmarkConsent_Revoke_Sets_Status_Revoked()
    {
        var consent = TenantBenchmarkConsent.RequestConsent("tenant-1", "Art. 7, VI LGPD");
        consent.Grant("admin", Now);
        consent.Revoke("admin", Now.AddDays(10));

        consent.Status.Should().Be(BenchmarkConsentStatus.Revoked);
        consent.RevokedAt.Should().Be(Now.AddDays(10));
    }

    [Fact]
    public void TenantBenchmarkConsent_IsOptedIn_True_When_Granted()
    {
        var consent = TenantBenchmarkConsent.RequestConsent("tenant-1", "basis");
        consent.Grant("admin", Now);

        consent.IsOptedIn.Should().BeTrue();
    }

    [Fact]
    public void TenantBenchmarkConsent_IsOptedIn_False_When_Revoked()
    {
        var consent = TenantBenchmarkConsent.RequestConsent("tenant-1", "basis");
        consent.Grant("admin", Now);
        consent.Revoke("admin", Now.AddDays(1));

        consent.IsOptedIn.Should().BeFalse();
    }

    [Fact]
    public void BenchmarkSnapshotRecord_Record_Creates_With_Correct_Metrics()
    {
        var snapshot = BenchmarkSnapshotRecord.Record(
            "tenant-1", Now.AddDays(-30), Now, 5m, 4m, 2m, 1m, 75m, 10, Now);

        snapshot.TenantId.Should().Be("tenant-1");
        snapshot.DeploymentFrequencyPerWeek.Should().Be(5m);
        snapshot.LeadTimeForChangesHours.Should().Be(4m);
        snapshot.ChangeFailureRatePercent.Should().Be(2m);
        snapshot.MeanTimeToRestoreHours.Should().Be(1m);
        snapshot.MaturityScore.Should().Be(75m);
        snapshot.ServiceCount.Should().Be(10);
        snapshot.IsAnonymizedForBenchmarks.Should().BeFalse();
    }

    [Fact]
    public void BenchmarkSnapshotRecord_MarkAsAnonymized_Sets_Flag()
    {
        var snapshot = BenchmarkSnapshotRecord.Record(
            "tenant-1", Now.AddDays(-30), Now, 5m, 4m, 2m, 1m, 75m, 10, Now);
        snapshot.MarkAsAnonymized();

        snapshot.IsAnonymizedForBenchmarks.Should().BeTrue();
    }

    // ── Handler tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task RecordBenchmarkConsent_Handler_Creates_New_Consent_On_Request()
    {
        _consentRepo.GetByTenantIdAsync("tenant-1", Arg.Any<CancellationToken>()).Returns((TenantBenchmarkConsent?)null);

        var handler = new RecordBenchmarkConsent.Handler(_consentRepo, _uow, _clock);
        var result = await handler.Handle(new RecordBenchmarkConsent.Command("tenant-1", RecordBenchmarkConsent.ConsentAction.Request, "Art. 7 LGPD"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(BenchmarkConsentStatus.Pending);
        _consentRepo.Received(1).Add(Arg.Any<TenantBenchmarkConsent>());
    }

    [Fact]
    public async Task RecordBenchmarkConsent_Handler_Grants_Existing_Consent()
    {
        var existing = TenantBenchmarkConsent.RequestConsent("tenant-1", "basis");
        _consentRepo.GetByTenantIdAsync("tenant-1", Arg.Any<CancellationToken>()).Returns(existing);

        var handler = new RecordBenchmarkConsent.Handler(_consentRepo, _uow, _clock);
        var result = await handler.Handle(new RecordBenchmarkConsent.Command("tenant-1", RecordBenchmarkConsent.ConsentAction.Grant, null, "admin"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsOptedIn.Should().BeTrue();
        result.Value.Status.Should().Be(BenchmarkConsentStatus.Granted);
    }

    [Fact]
    public async Task RecordBenchmarkConsent_Handler_Revokes_Existing_Consent()
    {
        var existing = TenantBenchmarkConsent.RequestConsent("tenant-1", "basis");
        existing.Grant("admin", Now);
        _consentRepo.GetByTenantIdAsync("tenant-1", Arg.Any<CancellationToken>()).Returns(existing);

        var handler = new RecordBenchmarkConsent.Handler(_consentRepo, _uow, _clock);
        var result = await handler.Handle(new RecordBenchmarkConsent.Command("tenant-1", RecordBenchmarkConsent.ConsentAction.Revoke, null, "admin"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(BenchmarkConsentStatus.Revoked);
        result.Value.IsOptedIn.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitBenchmarkSnapshot_Handler_Returns_ConsentNotGranted_When_No_Consent()
    {
        _consentRepo.GetByTenantIdAsync("tenant-1", Arg.Any<CancellationToken>()).Returns((TenantBenchmarkConsent?)null);

        var handler = new SubmitBenchmarkSnapshot.Handler(_consentRepo, _snapshotRepo, _uow, _clock);
        var result = await handler.Handle(
            new SubmitBenchmarkSnapshot.Command("tenant-1", Now.AddDays(-30), Now, 5m, 4m, 2m, 1m, 75m, 10, false),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ConsentNotGranted");
    }

    [Fact]
    public async Task SubmitBenchmarkSnapshot_Handler_Creates_Snapshot_When_Consented()
    {
        var consent = TenantBenchmarkConsent.RequestConsent("tenant-1", "basis");
        consent.Grant("admin", Now);
        _consentRepo.GetByTenantIdAsync("tenant-1", Arg.Any<CancellationToken>()).Returns(consent);

        var handler = new SubmitBenchmarkSnapshot.Handler(_consentRepo, _snapshotRepo, _uow, _clock);
        var result = await handler.Handle(
            new SubmitBenchmarkSnapshot.Command("tenant-1", Now.AddDays(-30), Now, 5m, 4m, 2m, 1m, 75m, 10, true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsAnonymizedForBenchmarks.Should().BeTrue();
        _snapshotRepo.Received(1).Add(Arg.Any<BenchmarkSnapshotRecord>());
    }

    [Fact]
    public async Task GetCrossRankedBenchmark_Handler_Returns_Percentile_Rankings()
    {
        var since = Now.AddDays(-90);
        var tenantSnapshot = BuildSnapshot("tenant-1", 8m, 2m, 1m, 0.5m, 90m, true);
        var peer1 = BuildSnapshot("tenant-2", 4m, 6m, 3m, 2m, 60m, true);
        var peer2 = BuildSnapshot("tenant-3", 6m, 4m, 2m, 1m, 75m, true);
        var peer3 = BuildSnapshot("tenant-4", 3m, 8m, 4m, 3m, 50m, true);
        var peer4 = BuildSnapshot("tenant-5", 5m, 5m, 2.5m, 1.5m, 70m, true);
        var peer5 = BuildSnapshot("tenant-6", 7m, 3m, 1.5m, 0.8m, 85m, true);

        _snapshotRepo.ListByTenantAsync("tenant-1", Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([tenantSnapshot]);
        _snapshotRepo.ListAnonymizedAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([tenantSnapshot, peer1, peer2, peer3, peer4, peer5]);

        var handler = new GetCrossRankedBenchmark.Handler(_snapshotRepo, _clock);
        var result = await handler.Handle(new GetCrossRankedBenchmark.Query("tenant-1", 90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.InsufficientPeers.Should().BeFalse();
        result.Value.PeerSetSize.Should().Be(5);
        result.Value.DeployFreqPercentile.Should().NotBeNull();
        result.Value.MaturityPercentile.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCrossRankedBenchmark_Handler_Returns_Empty_When_InsufficientPeers()
    {
        var tenantSnapshot = BuildSnapshot("tenant-1", 8m, 2m, 1m, 0.5m, 90m, true);

        _snapshotRepo.ListByTenantAsync("tenant-1", Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([tenantSnapshot]);
        // Only 2 peers — below DefaultMinPeerSetSize of 5
        _snapshotRepo.ListAnonymizedAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([tenantSnapshot, BuildSnapshot("tenant-2", 5m, 4m, 2m, 1m, 70m, true), BuildSnapshot("tenant-3", 6m, 3m, 2m, 1m, 75m, true)]);

        var handler = new GetCrossRankedBenchmark.Handler(_snapshotRepo, _clock);
        var result = await handler.Handle(new GetCrossRankedBenchmark.Query("tenant-1", 90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.InsufficientPeers.Should().BeTrue();
        result.Value.DeployFreqPercentile.Should().BeNull();
    }

    [Fact]
    public void BenchmarkConsentStatus_Has_Expected_Values()
    {
        ((int)BenchmarkConsentStatus.NotRequested).Should().Be(0);
        ((int)BenchmarkConsentStatus.Pending).Should().Be(1);
        ((int)BenchmarkConsentStatus.Granted).Should().Be(2);
        ((int)BenchmarkConsentStatus.Revoked).Should().Be(3);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private BenchmarkSnapshotRecord BuildSnapshot(
        string tenantId, decimal deployFreq, decimal leadTime, decimal failureRate,
        decimal mttr, decimal maturity, bool anonymized)
    {
        var snap = BenchmarkSnapshotRecord.Record(tenantId, Now.AddDays(-30), Now,
            deployFreq, leadTime, failureRate, mttr, maturity, 5, Now);
        if (anonymized) snap.MarkAsAnonymized();
        return snap;
    }
}
