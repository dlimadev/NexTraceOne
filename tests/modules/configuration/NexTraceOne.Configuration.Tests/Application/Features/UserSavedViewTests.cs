using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

using CreateFeature = NexTraceOne.Configuration.Application.Features.CreateUserSavedView.CreateUserSavedView;
using ListFeature = NexTraceOne.Configuration.Application.Features.ListUserSavedViews.ListUserSavedViews;

namespace NexTraceOne.Configuration.Tests.Application.Features;

/// <summary>
/// Testes de CreateUserSavedView e ListUserSavedViews — vistas guardadas por utilizador.
/// </summary>
public sealed class UserSavedViewTests
{
    private static ICurrentUser CreateAuthenticatedUser(string id = "user-123")
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(true);
        user.Id.Returns(id);
        user.Name.Returns("Test User");
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

    // ── CreateUserSavedView ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateSavedView_Should_Persist_New_View()
    {
        var repo = Substitute.For<IUserSavedViewRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();
        var uow = Substitute.For<IUnitOfWork>();
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var sut = new CreateFeature.Handler(repo, currentUser, currentTenant, uow);
        var result = await sut.Handle(
            new CreateFeature.Command("catalog.services", "My View", null, "{}", false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Context.Should().Be("catalog.services");
        result.Value.Name.Should().Be("My View");
        await repo.Received(1).AddAsync(Arg.Any<UserSavedView>(), Arg.Any<CancellationToken>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateSavedView_Should_Fail_When_Not_Authenticated()
    {
        var repo = Substitute.For<IUserSavedViewRepository>();
        var currentUser = CreateAnonymousUser();
        var currentTenant = CreateTenant();
        var uow = Substitute.For<IUnitOfWork>();

        var sut = new CreateFeature.Handler(repo, currentUser, currentTenant, uow);
        var result = await sut.Handle(
            new CreateFeature.Command("catalog.services", "My View", null, "{}", false),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotAuthenticated");
    }

    // ── ListUserSavedViews ───────────────────────────────────────────────────

    [Fact]
    public async Task ListSavedViews_Should_Return_User_Views()
    {
        var repo = Substitute.For<IUserSavedViewRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();

        var views = new List<UserSavedView>
        {
            UserSavedView.Create("user-123", "00000000-0000-0000-0000-000000000001", "catalog.services", "View A", "{}"),
            UserSavedView.Create("user-123", "00000000-0000-0000-0000-000000000001", "catalog.services", "View B", "{}"),
        };
        repo.ListByUserAsync("user-123", "catalog.services", Arg.Any<CancellationToken>()).Returns(views);
        repo.ListSharedByContextAsync("catalog.services", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserSavedView>());

        var sut = new ListFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(new ListFeature.Query("catalog.services"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Should().AllSatisfy(v => v.IsOwn.Should().BeTrue());
    }

    [Fact]
    public async Task ListSavedViews_Should_Include_Shared_Views()
    {
        var repo = Substitute.For<IUserSavedViewRepository>();
        var currentUser = CreateAuthenticatedUser("user-123");
        var currentTenant = CreateTenant();

        var ownViews = new List<UserSavedView>
        {
            UserSavedView.Create("user-123", "00000000-0000-0000-0000-000000000001", "catalog.services", "My View", "{}"),
        };
        var sharedViews = new List<UserSavedView>
        {
            UserSavedView.Create("user-999", "00000000-0000-0000-0000-000000000001", "catalog.services", "Team View", "{}", isShared: true),
        };
        repo.ListByUserAsync("user-123", "catalog.services", Arg.Any<CancellationToken>()).Returns(ownViews);
        repo.ListSharedByContextAsync("catalog.services", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(sharedViews);

        var sut = new ListFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(new ListFeature.Query("catalog.services"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Count(v => v.IsOwn).Should().Be(1);
        result.Value.Items.Count(v => !v.IsOwn).Should().Be(1);
    }
}
