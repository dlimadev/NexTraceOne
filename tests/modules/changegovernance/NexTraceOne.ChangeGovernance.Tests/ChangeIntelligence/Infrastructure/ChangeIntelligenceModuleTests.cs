using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Contracts.ChangeIntelligence.ServiceInterfaces;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Infrastructure;

public sealed class ChangeIntelligenceModuleTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

    // ── GetReleaseAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetReleaseAsync_WhenReleaseExists_ShouldReturnMappedDto()
    {
        await using var db = CreateDbContext();
        var release = Release.Create(Guid.NewGuid(), Guid.Empty, "PaymentService", "2.1.0", "production", "https://ci/pipe/1", "abc123", FixedNow);
        db.Releases.Add(release);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetReleaseAsync(release.Id.Value, CancellationToken.None);

        result.Should().NotBeNull();
        result!.ReleaseId.Should().Be(release.Id.Value);
        result.ServiceName.Should().Be("PaymentService");
        result.Version.Should().Be("2.1.0");
        result.Environment.Should().Be("production");
    }

    [Fact]
    public async Task GetReleaseAsync_WhenReleaseDoesNotExist_ShouldReturnNull()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var result = await sut.GetReleaseAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetReleaseAsync_ShouldReturnCorrectStatusAndChangeLevel()
    {
        await using var db = CreateDbContext();
        var release = Release.Create(Guid.NewGuid(), Guid.Empty, "OrderService", "1.0.0", "staging", "https://ci/pipe/2", "def456", FixedNow);
        db.Releases.Add(release);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetReleaseAsync(release.Id.Value, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Status.Should().NotBeNullOrEmpty();
        result.ChangeLevel.Should().NotBeNullOrEmpty();
        result.CreatedAt.Should().Be(FixedNow);
    }

    // ── GetChangeScoreAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetChangeScoreAsync_WhenScoreExists_ShouldReturnLatestScore()
    {
        await using var db = CreateDbContext();
        var release = Release.Create(Guid.NewGuid(), Guid.Empty, "InventoryService", "3.0.0", "staging", "https://ci/pipe/3", "aaa111", FixedNow);
        db.Releases.Add(release);

        var score = ChangeIntelligenceScore.Compute(release.Id, 0.8m, 0.5m, 0.9m, FixedNow, "auto");
        db.ChangeScores.Add(score);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetChangeScoreAsync(release.Id.Value, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeInRange(0m, 1m);
    }

    [Fact]
    public async Task GetChangeScoreAsync_WhenNoScoreExists_ShouldReturnNull()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var result = await sut.GetChangeScoreAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetChangeScoreAsync_WhenMultipleScoresExist_ShouldReturnMostRecent()
    {
        await using var db = CreateDbContext();
        var release = Release.Create(Guid.NewGuid(), Guid.Empty, "ShippingService", "1.2.0", "staging", "https://ci/pipe/4", "bbb222", FixedNow);
        db.Releases.Add(release);

        var olderScore = ChangeIntelligenceScore.Compute(release.Id, 0.2m, 0.2m, 0.2m, FixedNow.AddHours(-2), "auto");
        var newerScore = ChangeIntelligenceScore.Compute(release.Id, 0.9m, 0.9m, 0.9m, FixedNow, "auto");
        db.ChangeScores.AddRange(olderScore, newerScore);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetChangeScoreAsync(release.Id.Value, CancellationToken.None);

        // Must return the most recently computed score
        result.Should().NotBeNull();
        result!.Value.Should().BeGreaterThan(0.5m);
    }

    // ── GetBlastRadiusAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetBlastRadiusAsync_WhenReportExists_ShouldReturnMappedDto()
    {
        await using var db = CreateDbContext();
        var release = Release.Create(Guid.NewGuid(), Guid.Empty, "CatalogService", "4.0.0", "production", "https://ci/pipe/5", "ccc333", FixedNow);
        db.Releases.Add(release);

        var report = BlastRadiusReport.Calculate(
            release.Id,
            release.ApiAssetId,
            ["ConsumerA", "ConsumerB"],
            ["TransitiveX"],
            FixedNow);
        db.BlastRadiusReports.Add(report);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetBlastRadiusAsync(release.Id.Value, CancellationToken.None);

        result.Should().NotBeNull();
        result!.ReleaseId.Should().Be(release.Id.Value);
        result.TotalAffectedConsumers.Should().Be(3);
        result.DirectConsumers.Should().BeEquivalentTo(["ConsumerA", "ConsumerB"]);
        result.TransitiveConsumers.Should().BeEquivalentTo(["TransitiveX"]);
    }

    [Fact]
    public async Task GetBlastRadiusAsync_WhenNoReportExists_ShouldReturnNull()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var result = await sut.GetBlastRadiusAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBlastRadiusAsync_WhenMultipleReportsExist_ShouldReturnMostRecent()
    {
        await using var db = CreateDbContext();
        var release = Release.Create(Guid.NewGuid(), Guid.Empty, "NotificationService", "2.0.0", "staging", "https://ci/pipe/6", "ddd444", FixedNow);
        db.Releases.Add(release);

        var olderReport = BlastRadiusReport.Calculate(release.Id, release.ApiAssetId, ["OldConsumer"], [], FixedNow.AddHours(-3));
        var newerReport = BlastRadiusReport.Calculate(release.Id, release.ApiAssetId, ["NewConsumer1", "NewConsumer2"], ["Trans1"], FixedNow);
        db.BlastRadiusReports.AddRange(olderReport, newerReport);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetBlastRadiusAsync(release.Id.Value, CancellationToken.None);

        result.Should().NotBeNull();
        result!.TotalAffectedConsumers.Should().Be(3);
        result.DirectConsumers.Should().BeEquivalentTo(["NewConsumer1", "NewConsumer2"]);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ChangeIntelligenceDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ChangeIntelligenceDbContext>()
            .UseInMemoryDatabase($"chg-module-tests-{Guid.NewGuid():N}")
            .Options;

        return new ChangeIntelligenceDbContext(
            options,
            new TestCurrentTenant(),
            new TestCurrentUser(),
            new TestDateTimeProvider());
    }

    private static IChangeIntelligenceModule CreateSut(ChangeIntelligenceDbContext db)
        => new ChangeIntelligenceModule(db, NullLogger<ChangeIntelligenceModule>.Instance);

    private sealed class TestCurrentTenant : ICurrentTenant
    {
        public Guid Id => Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        public string Slug => "tests";
        public string Name => "Tests";
        public bool IsActive => true;
        public bool HasCapability(string capability) => true;
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public string Id => "chg-module-tests-user";
        public string Name => "ChangeIntelligence Tests";
        public string Email => "chg.tests@nextraceone.local";
        public bool IsAuthenticated => true;
        public bool HasPermission(string permission) => true;
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => FixedNow;
        public DateOnly UtcToday => DateOnly.FromDateTime(FixedNow.UtcDateTime);
    }
}
