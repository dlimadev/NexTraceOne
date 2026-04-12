using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

using AddFeature = NexTraceOne.Configuration.Application.Features.AddBookmark.AddBookmark;
using ListFeature = NexTraceOne.Configuration.Application.Features.ListBookmarks.ListBookmarks;

namespace NexTraceOne.Configuration.Tests.Application.Features;

/// <summary>
/// Testes de AddBookmark e ListBookmarks — gestão de favoritos de entidades da plataforma por utilizador.
/// </summary>
public sealed class BookmarkTests
{
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

    // ── AddBookmark ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AddBookmark_Should_Create_New_Bookmark()
    {
        var repo = Substitute.For<IUserBookmarkRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();
        var uow = Substitute.For<IUnitOfWork>();
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        repo.FindAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<BookmarkEntityType>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((UserBookmark?)null);

        var sut = new AddFeature.Handler(repo, currentUser, currentTenant, uow);
        var result = await sut.Handle(
            new AddFeature.Command(BookmarkEntityType.Service, "svc-001", "My Service", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EntityId.Should().Be("svc-001");
        result.Value.EntityType.Should().Be(BookmarkEntityType.Service);
        await repo.Received(1).AddAsync(Arg.Any<UserBookmark>(), Arg.Any<CancellationToken>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddBookmark_Should_Return_Existing_When_Already_Bookmarked()
    {
        var repo = Substitute.For<IUserBookmarkRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();
        var uow = Substitute.For<IUnitOfWork>();

        var existing = UserBookmark.Create("user-123", "00000000-0000-0000-0000-000000000001", BookmarkEntityType.Service, "svc-001", "My Service");
        repo.FindAsync(Arg.Any<string>(), Arg.Any<string>(), BookmarkEntityType.Service, "svc-001", Arg.Any<CancellationToken>())
            .Returns(existing);

        var sut = new AddFeature.Handler(repo, currentUser, currentTenant, uow);
        var result = await sut.Handle(
            new AddFeature.Command(BookmarkEntityType.Service, "svc-001", "My Service", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(existing.Id.Value);
        await repo.DidNotReceive().AddAsync(Arg.Any<UserBookmark>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddBookmark_Should_Fail_When_Not_Authenticated()
    {
        var repo = Substitute.For<IUserBookmarkRepository>();
        var currentUser = CreateAnonymousUser();
        var currentTenant = CreateTenant();
        var uow = Substitute.For<IUnitOfWork>();

        var sut = new AddFeature.Handler(repo, currentUser, currentTenant, uow);
        var result = await sut.Handle(
            new AddFeature.Command(BookmarkEntityType.Service, "svc-001", "My Service", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotAuthenticated");
    }

    // ── ListBookmarks ────────────────────────────────────────────────────────

    [Fact]
    public async Task ListBookmarks_Should_Return_User_Bookmarks()
    {
        var repo = Substitute.For<IUserBookmarkRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();

        var bookmarks = new List<UserBookmark>
        {
            UserBookmark.Create("user-123", "00000000-0000-0000-0000-000000000001", BookmarkEntityType.Service, "svc-001", "Service A"),
            UserBookmark.Create("user-123", "00000000-0000-0000-0000-000000000001", BookmarkEntityType.Change, "chg-001", "Change B"),
        };
        repo.ListByUserAsync("user-123", Arg.Any<string?>(), null, Arg.Any<CancellationToken>())
            .Returns(bookmarks);

        var sut = new ListFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(new ListFeature.Query(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListBookmarks_Should_Filter_By_EntityType()
    {
        var repo = Substitute.For<IUserBookmarkRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();

        var bookmarks = new List<UserBookmark>
        {
            UserBookmark.Create("user-123", "00000000-0000-0000-0000-000000000001", BookmarkEntityType.Service, "svc-001", "Service A"),
        };
        repo.ListByUserAsync("user-123", Arg.Any<string?>(), BookmarkEntityType.Service, Arg.Any<CancellationToken>())
            .Returns(bookmarks);

        var sut = new ListFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(new ListFeature.Query(BookmarkEntityType.Service), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].EntityType.Should().Be(BookmarkEntityType.Service);
    }

    [Fact]
    public async Task ListBookmarks_Should_Fail_When_Not_Authenticated()
    {
        var repo = Substitute.For<IUserBookmarkRepository>();
        var currentUser = CreateAnonymousUser();
        var currentTenant = CreateTenant();

        var sut = new ListFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(new ListFeature.Query(null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
