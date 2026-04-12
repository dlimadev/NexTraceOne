using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

using SaveFeature = NexTraceOne.Configuration.Application.Features.SavePrompt.SavePrompt;
using ListFeature = NexTraceOne.Configuration.Application.Features.ListSavedPrompts.ListSavedPrompts;
using DeleteFeature = NexTraceOne.Configuration.Application.Features.DeleteSavedPrompt.DeleteSavedPrompt;
using ShareFeature = NexTraceOne.Configuration.Application.Features.ShareSavedPrompt.ShareSavedPrompt;

namespace NexTraceOne.Configuration.Tests.Application.Features;

/// <summary>
/// Testes de SavePrompt, ListSavedPrompts, DeleteSavedPrompt e ShareSavedPrompt —
/// gestão de prompts de IA guardados pelo utilizador.
/// </summary>
public sealed class SavedPromptTests
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

    // ── SavePrompt ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SavePrompt_Should_Create_When_Authenticated()
    {
        var repo = Substitute.For<ISavedPromptRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();
        var clock = CreateClock();

        var sut = new SaveFeature.Handler(repo, currentUser, currentTenant, clock);
        var result = await sut.Handle(
            new SaveFeature.Command("Incident triage", "Analyze the incident and suggest root cause", "incident", "ops,triage", false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Incident triage");
        result.Value.ContextType.Should().Be("incident");
        result.Value.IsShared.Should().BeFalse();
        result.Value.CreatedAt.Should().Be(FixedNow);
        await repo.Received(1).AddAsync(Arg.Any<SavedPrompt>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavePrompt_Should_Fail_When_Not_Authenticated()
    {
        var repo = Substitute.For<ISavedPromptRepository>();
        var currentUser = CreateAnonymousUser();
        var currentTenant = CreateTenant();
        var clock = CreateClock();

        var sut = new SaveFeature.Handler(repo, currentUser, currentTenant, clock);
        var result = await sut.Handle(
            new SaveFeature.Command("Prompt", "Some text", "general", null, false),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotAuthenticated");
    }

    // ── ListSavedPrompts ─────────────────────────────────────────────────────

    [Fact]
    public async Task ListSavedPrompts_Should_Return_Prompts()
    {
        var repo = Substitute.For<ISavedPromptRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();

        var prompts = new List<SavedPrompt>
        {
            SavedPrompt.Create("user-123", "00000000-0000-0000-0000-000000000001", "Prompt A", "Text A", "general", null, false, FixedNow),
            SavedPrompt.Create("user-123", "00000000-0000-0000-0000-000000000001", "Prompt B", "Text B", "incident", "ops", true, FixedNow),
        };
        repo.ListByUserAsync("user-123", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(prompts);

        var sut = new ListFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(new ListFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListSavedPrompts_Should_Return_Empty_When_No_Prompts()
    {
        var repo = Substitute.For<ISavedPromptRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();

        repo.ListByUserAsync("user-123", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<SavedPrompt>());

        var sut = new ListFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(new ListFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    // ── DeleteSavedPrompt ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteSavedPrompt_Should_Delete_When_Owner()
    {
        var repo = Substitute.For<ISavedPromptRepository>();
        var currentUser = CreateAuthenticatedUser();

        var prompt = SavedPrompt.Create("user-123", "00000000-0000-0000-0000-000000000001", "My Prompt", "Some text", "general", null, false, FixedNow);
        repo.GetByIdAsync(Arg.Any<SavedPromptId>(), Arg.Any<CancellationToken>())
            .Returns(prompt);

        var sut = new DeleteFeature.Handler(repo, currentUser);
        var result = await sut.Handle(
            new DeleteFeature.Command(prompt.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PromptId.Should().Be(prompt.Id.Value);
        await repo.Received(1).DeleteAsync(Arg.Any<SavedPromptId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteSavedPrompt_Should_Fail_When_Not_Found()
    {
        var repo = Substitute.For<ISavedPromptRepository>();
        var currentUser = CreateAuthenticatedUser();

        repo.GetByIdAsync(Arg.Any<SavedPromptId>(), Arg.Any<CancellationToken>())
            .Returns((SavedPrompt?)null);

        var sut = new DeleteFeature.Handler(repo, currentUser);
        var result = await sut.Handle(
            new DeleteFeature.Command(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    // ── ShareSavedPrompt ─────────────────────────────────────────────────────

    [Fact]
    public async Task ShareSavedPrompt_Should_Update_When_Owner()
    {
        var repo = Substitute.For<ISavedPromptRepository>();
        var currentUser = CreateAuthenticatedUser();

        var prompt = SavedPrompt.Create("user-123", "00000000-0000-0000-0000-000000000001", "My Prompt", "Some text", "general", null, false, FixedNow);
        repo.GetByIdAsync(Arg.Any<SavedPromptId>(), Arg.Any<CancellationToken>())
            .Returns(prompt);

        var sut = new ShareFeature.Handler(repo, currentUser);
        var result = await sut.Handle(
            new ShareFeature.Command(prompt.Id.Value, true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PromptId.Should().Be(prompt.Id.Value);
        result.Value.IsShared.Should().BeTrue();
        await repo.Received(1).UpdateAsync(Arg.Any<SavedPrompt>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShareSavedPrompt_Should_Fail_When_Not_Found()
    {
        var repo = Substitute.For<ISavedPromptRepository>();
        var currentUser = CreateAuthenticatedUser();

        repo.GetByIdAsync(Arg.Any<SavedPromptId>(), Arg.Any<CancellationToken>())
            .Returns((SavedPrompt?)null);

        var sut = new ShareFeature.Handler(repo, currentUser);
        var result = await sut.Handle(
            new ShareFeature.Command(Guid.NewGuid(), true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }
}
