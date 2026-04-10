using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

using CreateFeature = NexTraceOne.Configuration.Application.Features.CreateAutomationRule.CreateAutomationRule;
using DeleteFeature = NexTraceOne.Configuration.Application.Features.DeleteAutomationRule.DeleteAutomationRule;
using ListFeature = NexTraceOne.Configuration.Application.Features.ListAutomationRules.ListAutomationRules;
using ToggleFeature = NexTraceOne.Configuration.Application.Features.ToggleAutomationRule.ToggleAutomationRule;

namespace NexTraceOne.Configuration.Tests.Application.Features;

/// <summary>
/// Testes de CreateAutomationRule, DeleteAutomationRule, ListAutomationRules e ToggleAutomationRule —
/// gestão de regras de automação If-Then por tenant.
/// </summary>
public sealed class AutomationRuleTests
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

    // ── CreateAutomationRule ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateAutomationRule_Should_Create_When_Authenticated()
    {
        var repo = Substitute.For<IAutomationRuleRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();
        var clock = CreateClock();

        var sut = new CreateFeature.Handler(repo, currentUser, currentTenant, clock);
        var result = await sut.Handle(
            new CreateFeature.Command(
                "Auto-assign reviewer",
                "on_change_created",
                "[{\"field\":\"risk\",\"operator\":\">=\",\"value\":\"high\"}]",
                "[{\"type\":\"assign_reviewer\",\"target\":\"tech-lead\"}]"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Auto-assign reviewer");
        result.Value.Trigger.Should().Be("on_change_created");
        result.Value.IsEnabled.Should().BeTrue();
        result.Value.CreatedAt.Should().Be(FixedNow);
        await repo.Received(1).AddAsync(Arg.Any<AutomationRule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAutomationRule_Should_Fail_When_Not_Authenticated()
    {
        var repo = Substitute.For<IAutomationRuleRepository>();
        var currentUser = CreateAnonymousUser();
        var currentTenant = CreateTenant();
        var clock = CreateClock();

        var sut = new CreateFeature.Handler(repo, currentUser, currentTenant, clock);
        var result = await sut.Handle(
            new CreateFeature.Command("Rule", "on_change_created", "[]", "[]"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotAuthenticated");
    }

    // ── DeleteAutomationRule ──────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAutomationRule_Should_Delete_When_Found()
    {
        var repo = Substitute.For<IAutomationRuleRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();

        var rule = AutomationRule.Create(
            "00000000-0000-0000-0000-000000000001",
            "My Rule",
            "on_incident_opened",
            "[]",
            "[{\"type\":\"send_notification\"}]",
            "user-123",
            FixedNow);
        repo.GetByIdAsync(Arg.Any<AutomationRuleId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rule);

        var sut = new DeleteFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(
            new DeleteFeature.Command(rule.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RuleId.Should().Be(rule.Id.Value);
        await repo.Received(1).DeleteAsync(Arg.Any<AutomationRuleId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAutomationRule_Should_Fail_When_Not_Found()
    {
        var repo = Substitute.For<IAutomationRuleRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();

        repo.GetByIdAsync(Arg.Any<AutomationRuleId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((AutomationRule?)null);

        var sut = new DeleteFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(
            new DeleteFeature.Command(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    // ── ListAutomationRules ─────────────────────────────────────────────────

    [Fact]
    public async Task ListAutomationRules_Should_Return_Rules()
    {
        var repo = Substitute.For<IAutomationRuleRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();

        var rules = new List<AutomationRule>
        {
            AutomationRule.Create("00000000-0000-0000-0000-000000000001", "Rule A", "on_change_created", "[]", "[]", "user-123", FixedNow),
            AutomationRule.Create("00000000-0000-0000-0000-000000000001", "Rule B", "on_incident_opened", "[]", "[]", "user-123", FixedNow),
        };
        repo.ListByTenantAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rules);

        var sut = new ListFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(new ListFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListAutomationRules_Should_Fail_When_Not_Authenticated()
    {
        var repo = Substitute.For<IAutomationRuleRepository>();
        var currentUser = CreateAnonymousUser();
        var currentTenant = CreateTenant();

        var sut = new ListFeature.Handler(repo, currentUser, currentTenant);
        var result = await sut.Handle(new ListFeature.Query(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotAuthenticated");
    }

    // ── ToggleAutomationRule ────────────────────────────────────────────────

    [Fact]
    public async Task ToggleAutomationRule_Should_Toggle_When_Found()
    {
        var repo = Substitute.For<IAutomationRuleRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();
        var clock = CreateClock();

        var rule = AutomationRule.Create(
            "00000000-0000-0000-0000-000000000001",
            "My Rule",
            "on_change_created",
            "[]",
            "[]",
            "user-123",
            FixedNow);
        repo.GetByIdAsync(Arg.Any<AutomationRuleId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rule);

        var sut = new ToggleFeature.Handler(repo, currentUser, currentTenant, clock);
        var result = await sut.Handle(
            new ToggleFeature.Command(rule.Id.Value, false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        rule.IsEnabled.Should().BeFalse();
        await repo.Received(1).UpdateAsync(Arg.Any<AutomationRule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleAutomationRule_Should_Fail_When_Not_Found()
    {
        var repo = Substitute.For<IAutomationRuleRepository>();
        var currentUser = CreateAuthenticatedUser();
        var currentTenant = CreateTenant();
        var clock = CreateClock();

        repo.GetByIdAsync(Arg.Any<AutomationRuleId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((AutomationRule?)null);

        var sut = new ToggleFeature.Handler(repo, currentUser, currentTenant, clock);
        var result = await sut.Handle(
            new ToggleFeature.Command(Guid.NewGuid(), true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }
}
