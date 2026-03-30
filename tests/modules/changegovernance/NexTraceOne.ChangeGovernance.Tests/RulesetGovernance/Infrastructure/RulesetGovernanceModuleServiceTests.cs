using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Contracts.RulesetGovernance.ServiceInterfaces;
using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Entities;
using NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance.Services;

namespace NexTraceOne.ChangeGovernance.Tests.RulesetGovernance.Infrastructure;

public sealed class RulesetGovernanceModuleServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

    // ── GetRulesetScoreAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetRulesetScoreAsync_WhenLintResultExists_ShouldReturnMappedDto()
    {
        await using var db = CreateDbContext();
        var releaseId = Guid.NewGuid();

        var lintResult = LintResult.Create(
            RulesetId.New(),
            releaseId,
            Guid.NewGuid(),
            85.5m,
            [],
            FixedNow);

        db.LintResults.Add(lintResult);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.GetRulesetScoreAsync(releaseId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.LintResultId.Should().Be(lintResult.Id.Value);
        result.ReleaseId.Should().Be(releaseId);
        result.Score.Should().Be(85.5m);
        result.TotalFindings.Should().Be(0);
        result.ExecutedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task GetRulesetScoreAsync_WhenNoLintResultExists_ShouldReturnNull()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var result = await sut.GetRulesetScoreAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRulesetScoreAsync_WhenMultipleResults_ShouldReturnMostRecent()
    {
        await using var db = CreateDbContext();
        var releaseId = Guid.NewGuid();

        var olderResult = LintResult.Create(
            RulesetId.New(), releaseId, Guid.NewGuid(), 60m, [], FixedNow.AddHours(-2));
        var newerResult = LintResult.Create(
            RulesetId.New(), releaseId, Guid.NewGuid(), 92m, [], FixedNow);

        db.LintResults.AddRange(olderResult, newerResult);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.GetRulesetScoreAsync(releaseId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Score.Should().Be(92m);
        result.ExecutedAt.Should().Be(FixedNow);
    }

    // ── IsReleaseCompliantAsync ─────────────────────────────────────────────

    [Fact]
    public async Task IsReleaseCompliantAsync_WhenScoreAboveMinimum_ShouldReturnTrue()
    {
        await using var db = CreateDbContext();
        var releaseId = Guid.NewGuid();

        var lintResult = LintResult.Create(
            RulesetId.New(), releaseId, Guid.NewGuid(), 90m, [], FixedNow);

        db.LintResults.Add(lintResult);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.IsReleaseCompliantAsync(releaseId, 80m, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsReleaseCompliantAsync_WhenScoreBelowMinimum_ShouldReturnFalse()
    {
        await using var db = CreateDbContext();
        var releaseId = Guid.NewGuid();

        var lintResult = LintResult.Create(
            RulesetId.New(), releaseId, Guid.NewGuid(), 50m, [], FixedNow);

        db.LintResults.Add(lintResult);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.IsReleaseCompliantAsync(releaseId, 80m, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsReleaseCompliantAsync_WhenNoLintResult_ShouldReturnFalse()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var result = await sut.IsReleaseCompliantAsync(Guid.NewGuid(), 80m, CancellationToken.None);

        result.Should().BeFalse();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static RulesetGovernanceDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<RulesetGovernanceDbContext>()
            .UseInMemoryDatabase($"rg-module-tests-{Guid.NewGuid():N}")
            .Options;

        return new RulesetGovernanceDbContext(
            options,
            new TestCurrentTenant(),
            new TestCurrentUser(),
            new TestDateTimeProvider());
    }

    private static IRulesetGovernanceModule CreateSut(RulesetGovernanceDbContext db)
        => new RulesetGovernanceModuleService(db, NullLogger<RulesetGovernanceModuleService>.Instance);

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
        public string Id => "rg-module-tests-user";
        public string Name => "RulesetGovernance Tests";
        public string Email => "rg.tests@nextraceone.local";
        public bool IsAuthenticated => true;
        public bool HasPermission(string permission) => true;
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => FixedNow;
        public DateOnly UtcToday => DateOnly.FromDateTime(FixedNow.UtcDateTime);
    }
}
