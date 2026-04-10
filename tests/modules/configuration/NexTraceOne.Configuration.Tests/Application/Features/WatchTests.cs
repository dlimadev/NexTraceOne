using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

using WatchFeature = NexTraceOne.Configuration.Application.Features.WatchEntity.WatchEntity;
using UnwatchFeature = NexTraceOne.Configuration.Application.Features.UnwatchEntity.UnwatchEntity;
using ListFeature = NexTraceOne.Configuration.Application.Features.ListWatches.ListWatches;

namespace NexTraceOne.Configuration.Tests.Application.Features;

/// <summary>
/// Testes de WatchEntity, UnwatchEntity e ListWatches —
/// gestão de watch lists de entidades por utilizador.
/// </summary>
public sealed class WatchTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static ICurrentUser CreateAuthenticatedUser(string id = "user-123")
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(true);
        user.Id.Returns(id);
        user.Name.Returns("Test User");
        user.Email.Returns($"{id}@test.com");
        return user;
    }

    private static ICurrentUser CreateAnonymousUser()
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(false);
        return user;
    }

    private static ICurrentTenant CreateTenant(string id = "00000000-0000-0000-0000-000000000001")
    {
        var tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(Guid.Parse(id));
        return tenant;
    }

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    // ── WatchEntity ──────────────────────────────────────────────────────────

    [Fact]
    public async Task WatchEntity_Should_Create_When_Authenticated()
    {
        var repo = Substitute.For<IUserWatchRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();
        var clock = CreateClock();

        repo.GetByEntityAsync("user-123", Arg.Any<string>(), "service", "svc-1", Arg.Any<CancellationToken>())
            .Returns((UserWatch?)null);

        var sut = new WatchFeature.Handler(repo, currentUser, currentTenant, clock);
        var result = await sut.Handle(
            new WatchFeature.Command("service", "svc-1", "all"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EntityType.Should().Be("service");
        result.Value.EntityId.Should().Be("svc-1");
        result.Value.NotifyLevel.Should().Be("all");
        result.Value.CreatedAt.Should().Be(FixedNow);
        await repo.Received(1).AddAsync(Arg.Any<UserWatch>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WatchEntity_Should_Update_When_Already_Watching()
    {
        var repo = Substitute.For<IUserWatchRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();
        var clock = CreateClock();

        var existing = UserWatch.Create("user-123", "00000000-0000-0000-0000-000000000001", "service", "svc-1", "all", FixedNow);
        repo.GetByEntityAsync("user-123", Arg.Any<string>(), "service", "svc-1", Arg.Any<CancellationToken>())
            .Returns(existing);

        var sut = new WatchFeature.Handler(repo, currentUser, currentTenant, clock);
        var result = await sut.Handle(
            new WatchFeature.Command("service", "svc-1", "critical"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NotifyLevel.Should().Be("critical");
        await repo.Received(1).UpdateAsync(Arg.Any<UserWatch>(), Arg.Any<CancellationToken>());
        await repo.DidNotReceive().AddAsync(Arg.Any<UserWatch>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WatchEntity_Should_Fail_When_Not_Authenticated()
    {
        var repo = Substitute.For<IUserWatchRepository>();
        var currentUser = CreateAnonymousUser();
        var currentTenant = CreateTenant();
        var clock = CreateClock();

        var sut = new WatchFeature.Handler(repo, currentUser, currentTenant, clock);
        var result = await sut.Handle(
            new WatchFeature.Command("service", "svc-1", "all"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotAuthenticated");
    }

    // ── UnwatchEntity ────────────────────────────────────────────────────────

    [Fact]
    public async Task UnwatchEntity_Should_Delete_When_Owner()
    {
        var repo = Substitute.For<IUserWatchRepository>();
        var currentUser = CreateAuthenticatedUser();

        var watch = UserWatch.Create("user-123", "00000000-0000-0000-0000-000000000001", "service", "svc-1", "all", FixedNow);
        repo.GetByIdAsync(Arg.Any<UserWatchId>(), Arg.Any<CancellationToken>())
            .Returns(watch);

        var sut = new UnwatchFeature.Handler(repo, currentUser);
        var result = await sut.Handle(
            new UnwatchFeature.Command(watch.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.WatchId.Should().Be(watch.Id.Value);
        await repo.Received(1).DeleteAsync(Arg.Any<UserWatch>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnwatchEntity_Should_Fail_When_Not_Found()
    {
        var repo = Substitute.For<IUserWatchRepository>();
        var currentUser = CreateAuthenticatedUser();

        repo.GetByIdAsync(Arg.Any<UserWatchId>(), Arg.Any<CancellationToken>())
            .Returns((UserWatch?)null);

        var sut = new UnwatchFeature.Handler(repo, currentUser);
        var result = await sut.Handle(
            new UnwatchFeature.Command(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    // ── ListWatches ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ListWatches_Should_Return_Watches()
    {
        var repo = Substitute.For<IUserWatchRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();

        var watches = new List<UserWatch>
        {
            UserWatch.Create("user-123", "00000000-0000-0000-0000-000000000001", "service", "svc-1", "all", FixedNow),
            UserWatch.Create("user-123", "00000000-0000-0000-0000-000000000001", "contract", "ctr-2", "critical", FixedNow),
        };
        repo.ListByUserAsync("user-123", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(watches);

        var sut = new ListFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(new ListFeature.Query(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListWatches_Should_Return_Empty_When_No_Watches()
    {
        var repo = Substitute.For<IUserWatchRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();

        repo.ListByUserAsync("user-123", Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserWatch>());

        var sut = new ListFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(new ListFeature.Query(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }
}
