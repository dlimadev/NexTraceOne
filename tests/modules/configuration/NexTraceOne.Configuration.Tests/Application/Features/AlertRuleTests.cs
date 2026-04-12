using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

using CreateFeature = NexTraceOne.Configuration.Application.Features.CreateAlertRule.CreateAlertRule;
using DeleteFeature = NexTraceOne.Configuration.Application.Features.DeleteAlertRule.DeleteAlertRule;
using ListFeature = NexTraceOne.Configuration.Application.Features.ListAlertRules.ListAlertRules;
using ToggleFeature = NexTraceOne.Configuration.Application.Features.ToggleAlertRule.ToggleAlertRule;

namespace NexTraceOne.Configuration.Tests.Application.Features;

/// <summary>
/// Testes de CreateAlertRule, DeleteAlertRule, ListAlertRules e ToggleAlertRule —
/// gestão de regras de alerta personalizadas por utilizador.
/// </summary>
public sealed class AlertRuleTests
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

    // ── CreateAlertRule ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAlertRule_Should_Create_When_Authenticated()
    {
        var repo = Substitute.For<IUserAlertRuleRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();
        var clock = CreateClock();
        var eventBus = Substitute.For<IEventBus>();

        var sut = new CreateFeature.Handler(repo, currentUser, currentTenant, clock, eventBus);
        var result = await sut.Handle(
            new CreateFeature.Command("High Risk Alert", "{\"entity\":\"service\",\"field\":\"risk\",\"operator\":\">=\",\"value\":\"high\"}", "email"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("High Risk Alert");
        result.Value.Channel.Should().Be("email");
        result.Value.IsEnabled.Should().BeTrue();
        result.Value.CreatedAt.Should().Be(FixedNow);
        await repo.Received(1).AddAsync(Arg.Any<UserAlertRule>(), Arg.Any<CancellationToken>());
        await eventBus.Received(1).PublishAsync(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAlertRule_Should_Fail_When_Not_Authenticated()
    {
        var repo = Substitute.For<IUserAlertRuleRepository>();
        var currentUser = CreateAnonymousUser();
        var currentTenant = CreateTenant();
        var clock = CreateClock();
        var eventBus = Substitute.For<IEventBus>();

        var sut = new CreateFeature.Handler(repo, currentUser, currentTenant, clock, eventBus);
        var result = await sut.Handle(
            new CreateFeature.Command("Alert", "{\"entity\":\"service\"}", "in-app"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotAuthenticated");
    }

    // ── DeleteAlertRule ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAlertRule_Should_Delete_When_Owner()
    {
        var repo = Substitute.For<IUserAlertRuleRepository>();
        var currentUser = CreateAuthenticatedUser();
        var ruleId = Guid.NewGuid();

        var rule = UserAlertRule.Create("user-123", "00000000-0000-0000-0000-000000000001", "My Rule", "{}", "in-app", FixedNow);
        repo.GetByIdAsync(Arg.Any<UserAlertRuleId>(), Arg.Any<CancellationToken>())
            .Returns(rule);

        var sut = new DeleteFeature.Handler(repo, currentUser);
        var result = await sut.Handle(
            new DeleteFeature.Command(rule.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RuleId.Should().Be(rule.Id.Value);
        await repo.Received(1).DeleteAsync(Arg.Any<UserAlertRule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAlertRule_Should_Fail_When_Not_Found()
    {
        var repo = Substitute.For<IUserAlertRuleRepository>();
        var currentUser = CreateAuthenticatedUser();

        repo.GetByIdAsync(Arg.Any<UserAlertRuleId>(), Arg.Any<CancellationToken>())
            .Returns((UserAlertRule?)null);

        var sut = new DeleteFeature.Handler(repo, currentUser);
        var result = await sut.Handle(
            new DeleteFeature.Command(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task DeleteAlertRule_Should_Fail_When_Not_Owner()
    {
        var repo = Substitute.For<IUserAlertRuleRepository>();
        var currentUser = CreateAuthenticatedUser("other-user");

        var rule = UserAlertRule.Create("user-123", "00000000-0000-0000-0000-000000000001", "My Rule", "{}", "in-app", FixedNow);
        repo.GetByIdAsync(Arg.Any<UserAlertRuleId>(), Arg.Any<CancellationToken>())
            .Returns(rule);

        var sut = new DeleteFeature.Handler(repo, currentUser);
        var result = await sut.Handle(
            new DeleteFeature.Command(rule.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Forbidden");
    }

    // ── ListAlertRules ────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAlertRules_Should_Return_Rules()
    {
        var repo = Substitute.For<IUserAlertRuleRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();

        var rules = new List<UserAlertRule>
        {
            UserAlertRule.Create("user-123", "00000000-0000-0000-0000-000000000001", "Rule A", "{}", "in-app", FixedNow),
            UserAlertRule.Create("user-123", "00000000-0000-0000-0000-000000000001", "Rule B", "{}", "email", FixedNow),
        };
        repo.ListByUserAsync("user-123", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rules);

        var sut = new ListFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(new ListFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListAlertRules_Should_Fail_When_Not_Authenticated()
    {
        var repo = Substitute.For<IUserAlertRuleRepository>();
        var currentUser = CreateAnonymousUser();
        var currentTenant = CreateTenant();

        var sut = new ListFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(new ListFeature.Query(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotAuthenticated");
    }

    // ── ToggleAlertRule ───────────────────────────────────────────────────────

    [Fact]
    public async Task ToggleAlertRule_Should_Toggle_When_Owner()
    {
        var repo = Substitute.For<IUserAlertRuleRepository>();
        var currentUser = CreateAuthenticatedUser();
        var clock = CreateClock();

        var rule = UserAlertRule.Create("user-123", "00000000-0000-0000-0000-000000000001", "My Rule", "{}", "in-app", FixedNow);
        repo.GetByIdAsync(Arg.Any<UserAlertRuleId>(), Arg.Any<CancellationToken>())
            .Returns(rule);

        var sut = new ToggleFeature.Handler(repo, currentUser, clock);
        var result = await sut.Handle(
            new ToggleFeature.Command(rule.Id.Value, false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        rule.IsEnabled.Should().BeFalse();
        await repo.Received(1).UpdateAsync(Arg.Any<UserAlertRule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleAlertRule_Should_Fail_When_Not_Found()
    {
        var repo = Substitute.For<IUserAlertRuleRepository>();
        var currentUser = CreateAuthenticatedUser();
        var clock = CreateClock();

        repo.GetByIdAsync(Arg.Any<UserAlertRuleId>(), Arg.Any<CancellationToken>())
            .Returns((UserAlertRule?)null);

        var sut = new ToggleFeature.Handler(repo, currentUser, clock);
        var result = await sut.Handle(
            new ToggleFeature.Command(Guid.NewGuid(), true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }
}
