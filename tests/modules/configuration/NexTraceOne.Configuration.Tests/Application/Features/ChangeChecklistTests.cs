using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

using CreateFeature = NexTraceOne.Configuration.Application.Features.CreateChangeChecklist.CreateChangeChecklist;
using DeleteFeature = NexTraceOne.Configuration.Application.Features.DeleteChangeChecklist.DeleteChangeChecklist;
using ListFeature = NexTraceOne.Configuration.Application.Features.ListChangeLists.ListChangeLists;

namespace NexTraceOne.Configuration.Tests.Application.Features;

/// <summary>
/// Testes de CreateChangeChecklist, DeleteChangeChecklist e ListChangeLists —
/// gestão de checklists personalizadas para tipos de mudança.
/// </summary>
public sealed class ChangeChecklistTests
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

    // ── CreateChangeChecklist ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateChangeChecklist_Should_Create_When_Authenticated()
    {
        var repo = Substitute.For<IChangeChecklistRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();
        var clock = CreateClock();

        var sut = new CreateFeature.Handler(repo, currentUser, currentTenant, clock);
        var result = await sut.Handle(
            new CreateFeature.Command("Pre-Deploy Checklist", "standard", "production", true, ["Run smoke tests", "Verify rollback plan"]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Pre-Deploy Checklist");
        result.Value.ChangeType.Should().Be("standard");
        result.Value.IsRequired.Should().BeTrue();
        result.Value.CreatedAt.Should().Be(FixedNow);
        await repo.Received(1).AddAsync(Arg.Any<ChangeChecklist>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateChangeChecklist_Should_Fail_When_Not_Authenticated()
    {
        var repo = Substitute.For<IChangeChecklistRepository>();
        var currentUser = CreateAnonymousUser();
        var currentTenant = CreateTenant();
        var clock = CreateClock();

        var sut = new CreateFeature.Handler(repo, currentUser, currentTenant, clock);
        var result = await sut.Handle(
            new CreateFeature.Command("Checklist", "emergency", null, false, ["Item"]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotAuthenticated");
    }

    // ── DeleteChangeChecklist ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteChangeChecklist_Should_Delete_When_Found()
    {
        var repo = Substitute.For<IChangeChecklistRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();

        var checklist = ChangeChecklist.Create(
            "00000000-0000-0000-0000-000000000001",
            "Deploy Checklist",
            "standard",
            "production",
            true,
            ["Verify config"],
            FixedNow);

        repo.GetByIdAsync(Arg.Any<ChangeChecklistId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(checklist);

        var sut = new DeleteFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(
            new DeleteFeature.Command(checklist.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(checklist.Id.Value);
        await repo.Received(1).DeleteAsync(Arg.Any<ChangeChecklistId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteChangeChecklist_Should_Fail_When_Not_Found()
    {
        var repo = Substitute.For<IChangeChecklistRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();

        repo.GetByIdAsync(Arg.Any<ChangeChecklistId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ChangeChecklist?)null);

        var sut = new DeleteFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(
            new DeleteFeature.Command(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task DeleteChangeChecklist_Should_Fail_When_Not_Authenticated()
    {
        var repo = Substitute.For<IChangeChecklistRepository>();
        var currentUser = CreateAnonymousUser();
        var currentTenant = CreateTenant();

        var sut = new DeleteFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(
            new DeleteFeature.Command(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotAuthenticated");
    }

    // ── ListChangeLists ───────────────────────────────────────────────────────

    [Fact]
    public async Task ListChangeLists_Should_Return_Checklists()
    {
        var repo = Substitute.For<IChangeChecklistRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();

        var checklists = new List<ChangeChecklist>
        {
            ChangeChecklist.Create("00000000-0000-0000-0000-000000000001", "Pre-Deploy", "standard", "production", true, ["Smoke test"], FixedNow),
            ChangeChecklist.Create("00000000-0000-0000-0000-000000000001", "Post-Deploy", "standard", null, false, ["Verify logs"], FixedNow),
        };
        repo.ListByTenantAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(checklists);

        var sut = new ListFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(new ListFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListChangeLists_Should_Fail_When_Not_Authenticated()
    {
        var repo = Substitute.For<IChangeChecklistRepository>();
        var currentUser = CreateAnonymousUser();
        var currentTenant = CreateTenant();

        var sut = new ListFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(new ListFeature.Query(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotAuthenticated");
    }
}
