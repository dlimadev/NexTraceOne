using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.CreatePolicy;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListBudgets;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListPolicies;
using NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateBudget;
using NexTraceOne.AIKnowledge.Application.Governance.Features.UpdatePolicy;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes unitários para CreatePolicy, ListPolicies, UpdatePolicy, ListBudgets, UpdateBudget.
/// Cobre criação, listagem e atualização de políticas e budgets de IA.
/// </summary>
public sealed class PolicyBudgetGapsTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IAiAccessPolicyRepository _policyRepository = Substitute.For<IAiAccessPolicyRepository>();
    private readonly IAiBudgetRepository _budgetRepository = Substitute.For<IAiBudgetRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public PolicyBudgetGapsTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    // ── CreatePolicy ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePolicy_ValidCommand_PersistsAndReturnsId()
    {
        AIAccessPolicy? persisted = null;
        _policyRepository.AddAsync(
            Arg.Do<AIAccessPolicy>(p => persisted = p),
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var command = new CreatePolicy.Command(
            Name: "engineering-policy",
            Description: "Policy for engineering team",
            Scope: "team",
            ScopeValue: "engineering",
            AllowExternalAI: false,
            InternalOnly: true,
            MaxTokensPerRequest: 4096);

        var handler = new CreatePolicy.Handler(_policyRepository, _clock);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PolicyId.Should().NotBe(Guid.Empty);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("engineering-policy");
        persisted.Scope.Should().Be("team");
        persisted.ScopeValue.Should().Be("engineering");
        persisted.InternalOnly.Should().BeTrue();
        persisted.AllowExternalAI.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePolicy_AllowExternalAI_SetsFlag()
    {
        AIAccessPolicy? persisted = null;
        _policyRepository.AddAsync(
            Arg.Do<AIAccessPolicy>(p => persisted = p),
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var command = new CreatePolicy.Command(
            Name: "exec-policy",
            Description: "Executive access policy",
            Scope: "role",
            ScopeValue: "executive",
            AllowExternalAI: true,
            InternalOnly: false,
            MaxTokensPerRequest: 16384);

        var handler = new CreatePolicy.Handler(_policyRepository, _clock);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        persisted!.AllowExternalAI.Should().BeTrue();
        persisted.InternalOnly.Should().BeFalse();
        persisted.MaxTokensPerRequest.Should().Be(16384);
    }

    // ── ListPolicies ───────────────────────────────────────────────────────

    [Fact]
    public async Task ListPolicies_NoFilters_ReturnsAllPolicies()
    {
        var policies = new List<AIAccessPolicy>
        {
            CreatePolicy("policy-1", "team", "eng", allowExternal: false, internalOnly: true),
            CreatePolicy("policy-2", "role", "admin", allowExternal: true, internalOnly: false),
        };
        _policyRepository.ListAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(policies.AsReadOnly());

        var handler = new ListPolicies.Handler(_policyRepository);
        var result = await handler.Handle(new ListPolicies.Query(null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListPolicies_FilterByScope_PassesScopeToRepository()
    {
        var policies = new List<AIAccessPolicy>
        {
            CreatePolicy("team-policy", "team", "eng", allowExternal: false, internalOnly: true),
        };
        _policyRepository.ListAsync("team", null, Arg.Any<CancellationToken>())
            .Returns(policies.AsReadOnly());

        var handler = new ListPolicies.Handler(_policyRepository);
        var result = await handler.Handle(new ListPolicies.Query("team", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items[0].Scope.Should().Be("team");
    }

    [Fact]
    public async Task ListPolicies_FilterActiveOnly_PassesFlagToRepository()
    {
        _policyRepository.ListAsync(null, true, Arg.Any<CancellationToken>())
            .Returns(new List<AIAccessPolicy>().AsReadOnly());

        var handler = new ListPolicies.Handler(_policyRepository);
        var result = await handler.Handle(new ListPolicies.Query(null, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _policyRepository.Received(1).ListAsync(null, true, Arg.Any<CancellationToken>());
    }

    // ── UpdatePolicy ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePolicy_ValidCommand_UpdatesAndPersists()
    {
        var policyId = Guid.NewGuid();
        var policy = CreatePolicy("my-policy", "team", "eng", allowExternal: false, internalOnly: true);

        _policyRepository.GetByIdAsync(
            Arg.Is<AIAccessPolicyId>(x => x == AIAccessPolicyId.From(policyId)),
            Arg.Any<CancellationToken>())
            .Returns(policy);

        var command = new UpdatePolicy.Command(
            PolicyId: policyId,
            Description: "Updated description",
            AllowExternalAI: true,
            InternalOnly: false,
            MaxTokensPerRequest: 8192,
            EnvironmentRestrictions: null);

        var handler = new UpdatePolicy.Handler(_policyRepository);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _policyRepository.Received(1).UpdateAsync(policy, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdatePolicy_PolicyNotFound_ReturnsFailure()
    {
        _policyRepository.GetByIdAsync(Arg.Any<AIAccessPolicyId>(), Arg.Any<CancellationToken>())
            .Returns((AIAccessPolicy?)null);

        var command = new UpdatePolicy.Command(
            PolicyId: Guid.NewGuid(),
            Description: "Desc",
            AllowExternalAI: false,
            InternalOnly: true,
            MaxTokensPerRequest: 1024,
            EnvironmentRestrictions: null);

        var handler = new UpdatePolicy.Handler(_policyRepository);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Policy.NotFound");
    }

    // ── ListBudgets ────────────────────────────────────────────────────────

    [Fact]
    public async Task ListBudgets_NoFilters_ReturnsAllBudgets()
    {
        var budgets = new List<AIBudget>
        {
            CreateBudget("Budget A", "team", "eng"),
            CreateBudget("Budget B", "user", "user-1"),
        };
        _budgetRepository.ListAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(budgets.AsReadOnly());

        var handler = new ListBudgets.Handler(_budgetRepository);
        var result = await handler.Handle(new ListBudgets.Query(null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListBudgets_FilterByScope_PassesScopeToRepository()
    {
        var budgets = new List<AIBudget>
        {
            CreateBudget("Team Budget", "team", "eng"),
        };
        _budgetRepository.ListAsync("team", null, Arg.Any<CancellationToken>())
            .Returns(budgets.AsReadOnly());

        var handler = new ListBudgets.Handler(_budgetRepository);
        var result = await handler.Handle(new ListBudgets.Query("team", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items[0].Scope.Should().Be("team");
    }

    [Fact]
    public async Task ListBudgets_FilterActiveOnly_PassesFlagToRepository()
    {
        _budgetRepository.ListAsync(null, true, Arg.Any<CancellationToken>())
            .Returns(new List<AIBudget>().AsReadOnly());

        var handler = new ListBudgets.Handler(_budgetRepository);
        var result = await handler.Handle(new ListBudgets.Query(null, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _budgetRepository.Received(1).ListAsync(null, true, Arg.Any<CancellationToken>());
    }

    // ── UpdateBudget ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateBudget_ValidCommand_UpdatesAndPersists()
    {
        var budgetId = Guid.NewGuid();
        var budget = CreateBudget("My Budget", "team", "eng");

        _budgetRepository.GetByIdAsync(
            Arg.Is<AIBudgetId>(x => x == AIBudgetId.From(budgetId)),
            Arg.Any<CancellationToken>())
            .Returns(budget);

        var command = new UpdateBudget.Command(
            BudgetId: budgetId,
            MaxTokens: 200_000L,
            MaxRequests: 500,
            Period: "Monthly");

        var handler = new UpdateBudget.Handler(_budgetRepository);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _budgetRepository.Received(1).UpdateAsync(budget, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateBudget_BudgetNotFound_ReturnsFailure()
    {
        _budgetRepository.GetByIdAsync(Arg.Any<AIBudgetId>(), Arg.Any<CancellationToken>())
            .Returns((AIBudget?)null);

        var command = new UpdateBudget.Command(
            BudgetId: Guid.NewGuid(),
            MaxTokens: 50_000L,
            MaxRequests: 100,
            Period: "Daily");

        var handler = new UpdateBudget.Handler(_budgetRepository);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Budget.NotFound");
    }

    [Fact]
    public async Task UpdateBudget_OnlyMaxTokensProvided_UpdatesTokensKeepsOtherValues()
    {
        var budgetId = Guid.NewGuid();
        var budget = CreateBudget("Partial Budget", "user", "user-x");

        _budgetRepository.GetByIdAsync(
            Arg.Is<AIBudgetId>(x => x == AIBudgetId.From(budgetId)),
            Arg.Any<CancellationToken>())
            .Returns(budget);

        var command = new UpdateBudget.Command(
            BudgetId: budgetId,
            MaxTokens: 999_999L,
            MaxRequests: null,
            Period: null);

        var handler = new UpdateBudget.Handler(_budgetRepository);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _budgetRepository.Received(1).UpdateAsync(budget, Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static AIAccessPolicy CreatePolicy(
        string name, string scope, string scopeValue,
        bool allowExternal, bool internalOnly) =>
        AIAccessPolicy.Create(name, $"Description for {name}", scope, scopeValue,
            allowExternal, internalOnly, 4096, DateTimeOffset.UtcNow);

    private static AIBudget CreateBudget(string name, string scope, string scopeValue) =>
        AIBudget.Create(name, scope, scopeValue,
            BudgetPeriod.Monthly, 100_000L, 200, DateTimeOffset.UtcNow);
}
