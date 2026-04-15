using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Contracts.Promotion.ServiceInterfaces;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Enums;
using NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Services;

namespace NexTraceOne.ChangeGovernance.Tests.Promotion.Infrastructure;

public sealed class PromotionModuleServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

    // ── IsPromotionApprovedAsync ────────────────────────────────────────────

    [Fact]
    public async Task IsPromotionApprovedAsync_WhenApprovedRequestExists_ShouldReturnTrue()
    {
        await using var db = CreateDbContext();
        var releaseId = Guid.NewGuid();
        var targetEnvId = DeploymentEnvironmentId.New();

        var request = PromotionRequest.Create(
            releaseId,
            DeploymentEnvironmentId.New(),
            targetEnvId,
            "deployer@nextraceone.local",
            FixedNow);
        request.StartEvaluation();
        request.Approve(FixedNow);

        db.PromotionRequests.Add(request);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.IsPromotionApprovedAsync(releaseId, targetEnvId.Value, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsPromotionApprovedAsync_WhenPendingRequestExists_ShouldReturnFalse()
    {
        await using var db = CreateDbContext();
        var releaseId = Guid.NewGuid();
        var targetEnvId = DeploymentEnvironmentId.New();

        var request = PromotionRequest.Create(
            releaseId,
            DeploymentEnvironmentId.New(),
            targetEnvId,
            "deployer@nextraceone.local",
            FixedNow);

        db.PromotionRequests.Add(request);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.IsPromotionApprovedAsync(releaseId, targetEnvId.Value, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsPromotionApprovedAsync_WhenNoRequestExists_ShouldReturnFalse()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var result = await sut.IsPromotionApprovedAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().BeFalse();
    }

    // ── GetPromotionStatusAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetPromotionStatusAsync_WhenRequestExists_ShouldReturnStatusString()
    {
        await using var db = CreateDbContext();
        var request = PromotionRequest.Create(
            Guid.NewGuid(),
            DeploymentEnvironmentId.New(),
            DeploymentEnvironmentId.New(),
            "deployer@nextraceone.local",
            FixedNow);

        db.PromotionRequests.Add(request);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.GetPromotionStatusAsync(request.Id.Value, CancellationToken.None);

        result.Should().Be(PromotionStatus.Pending.ToString());
    }

    [Fact]
    public async Task GetPromotionStatusAsync_WhenRequestDoesNotExist_ShouldReturnNull()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var result = await sut.GetPromotionStatusAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static PromotionDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PromotionDbContext>()
            .UseInMemoryDatabase($"prm-module-tests-{Guid.NewGuid():N}")
            .Options;

        return new PromotionDbContext(
            options,
            new TestCurrentTenant(),
            new TestCurrentUser(),
            new TestDateTimeProvider());
    }

    private static IPromotionModule CreateSut(PromotionDbContext db)
        => new PromotionModuleService(db, NullLogger<PromotionModuleService>.Instance);

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
        public string Id => "prm-module-tests-user";
        public string Name => "Promotion Tests";
        public string Email => "prm.tests@nextraceone.local";
        public string? Persona { get; } = null;
        public bool IsAuthenticated => true;
        public bool HasPermission(string permission) => true;
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => FixedNow;
        public DateOnly UtcToday => DateOnly.FromDateTime(FixedNow.UtcDateTime);
    }
}
