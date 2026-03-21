using System.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Services;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Services;

/// <summary>
/// Testes unitários do AiModelAuthorizationService — avaliação de políticas de acesso
/// (AIAccessPolicy) por prioridade de escopo, filtragem de modelos e decisão de acesso.
/// </summary>
public sealed class AiModelAuthorizationServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly IAiAccessPolicyRepository _policyRepo = Substitute.For<IAiAccessPolicyRepository>();
    private readonly IAiModelRepository _modelRepo = Substitute.For<IAiModelRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ILogger<AiModelAuthorizationService> _logger = NullLogger<AiModelAuthorizationService>.Instance;

    private AiModelAuthorizationService CreateSut() =>
        new(_policyRepo, _modelRepo, _currentUser, _logger);

    // ── Helpers ──────────────────────────────────────────────────────────

    private static AIModel CreateModel(string name, bool isInternal, ModelStatus status = ModelStatus.Active)
    {
        return AIModel.Register(name, name, isInternal ? "Internal" : "External",
            ModelType.Chat, isInternal, "chat", 1, FixedNow);
    }

    private static AIAccessPolicy CreatePolicy(
        string scope, string scopeValue,
        bool allowExternalAI = true, bool internalOnly = false)
    {
        return AIAccessPolicy.Create(
            $"policy-{scope}-{scopeValue}", "Test policy",
            scope, scopeValue, allowExternalAI, internalOnly, 4096, FixedNow);
    }

    private void SetupUser(string id = "user-1", string email = "user@company.com")
    {
        _currentUser.Id.Returns(id);
        _currentUser.Email.Returns(email);
        _currentUser.IsAuthenticated.Returns(true);
    }

    private void SetupModels(params AIModel[] models)
    {
        _modelRepo.ListAsync(null, null, ModelStatus.Active, null, Arg.Any<CancellationToken>())
            .Returns(Array.AsReadOnly(models));
    }

    private void SetupPolicies(params AIAccessPolicy[] policies)
    {
        _policyRepo.ListAsync(null, true, Arg.Any<CancellationToken>())
            .Returns(Array.AsReadOnly(policies));
    }

    // ── GetAvailableModelsAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetAvailableModels_NoPolicies_ShouldReturnAllActiveModels()
    {
        SetupUser();
        var internalModel = CreateModel("local-llm", isInternal: true);
        var externalModel = CreateModel("gpt-4o", isInternal: false);
        SetupModels(internalModel, externalModel);
        SetupPolicies();

        var sut = CreateSut();
        var result = await sut.GetAvailableModelsAsync(CancellationToken.None);

        result.Models.Should().HaveCount(2);
        result.AllowExternalModels.Should().BeTrue();
        result.AppliedPolicyName.Should().BeNull();
    }

    [Fact]
    public async Task GetAvailableModels_InternalOnlyPolicy_ShouldExcludeExternalModels()
    {
        SetupUser();
        var internalModel = CreateModel("local-llm", isInternal: true);
        var externalModel = CreateModel("gpt-4o", isInternal: false);
        SetupModels(internalModel, externalModel);

        var policy = CreatePolicy("user", "user-1", allowExternalAI: false, internalOnly: true);
        SetupPolicies(policy);

        var sut = CreateSut();
        var result = await sut.GetAvailableModelsAsync(CancellationToken.None);

        result.Models.Should().HaveCount(1);
        result.Models[0].Name.Should().Be("local-llm");
        result.AllowExternalModels.Should().BeFalse();
        result.AppliedPolicyName.Should().Contain("user");
    }

    [Fact]
    public async Task GetAvailableModels_BlockedModelIds_ShouldExcludeBlockedModels()
    {
        SetupUser();
        var model1 = CreateModel("model-a", isInternal: true);
        var model2 = CreateModel("model-b", isInternal: true);
        SetupModels(model1, model2);

        var policy = CreatePolicy("user", "user-1");
        policy.SetBlockedModels(model1.Id.Value.ToString());
        SetupPolicies(policy);

        var sut = CreateSut();
        var result = await sut.GetAvailableModelsAsync(CancellationToken.None);

        result.Models.Should().HaveCount(1);
        result.Models[0].Name.Should().Be("model-b");
    }

    [Fact]
    public async Task GetAvailableModels_AllowedModelIds_ShouldOnlyIncludeAllowed()
    {
        SetupUser();
        var model1 = CreateModel("model-a", isInternal: true);
        var model2 = CreateModel("model-b", isInternal: true);
        var model3 = CreateModel("model-c", isInternal: true);
        SetupModels(model1, model2, model3);

        var policy = CreatePolicy("user", "user-1");
        policy.SetAllowedModels($"{model1.Id.Value},{model2.Id.Value}");
        SetupPolicies(policy);

        var sut = CreateSut();
        var result = await sut.GetAvailableModelsAsync(CancellationToken.None);

        result.Models.Should().HaveCount(2);
        result.Models.Select(m => m.Name).Should().Contain("model-a").And.Contain("model-b");
    }

    [Fact]
    public async Task GetAvailableModels_UserPolicyTakesPriorityOverRole()
    {
        SetupUser();
        _currentUser.HasPermission("admin").Returns(true);

        var model = CreateModel("internal-llm", isInternal: true);
        SetupModels(model);

        var userPolicy = CreatePolicy("user", "user-1", allowExternalAI: false, internalOnly: true);
        var rolePolicy = CreatePolicy("role", "admin", allowExternalAI: true, internalOnly: false);
        SetupPolicies(userPolicy, rolePolicy);

        var sut = CreateSut();
        var result = await sut.GetAvailableModelsAsync(CancellationToken.None);

        result.AppliedPolicyName.Should().Contain("user");
        result.AllowExternalModels.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailableModels_EmailMatchesUserPolicy()
    {
        SetupUser(email: "admin@company.com");

        var model = CreateModel("model-x", isInternal: true);
        SetupModels(model);

        var policy = CreatePolicy("user", "admin@company.com", internalOnly: true);
        SetupPolicies(policy);

        var sut = CreateSut();
        var result = await sut.GetAvailableModelsAsync(CancellationToken.None);

        result.AppliedPolicyName.Should().Contain("user");
    }

    // ── ValidateModelAccessAsync ────────────────────────────────────────

    [Fact]
    public async Task ValidateModelAccess_NoPolicy_ShouldAllow()
    {
        SetupUser();
        var model = CreateModel("open-model", isInternal: true);
        _modelRepo.GetByIdAsync(model.Id, Arg.Any<CancellationToken>()).Returns(model);
        SetupPolicies();

        var sut = CreateSut();
        var decision = await sut.ValidateModelAccessAsync(model.Id.Value, CancellationToken.None);

        decision.IsAllowed.Should().BeTrue();
        decision.DenialReason.Should().BeNull();
    }

    [Fact]
    public async Task ValidateModelAccess_ModelNotFound_ShouldDeny()
    {
        SetupUser();
        _modelRepo.GetByIdAsync(Arg.Any<AIModelId>(), Arg.Any<CancellationToken>()).Returns((AIModel?)null);

        var sut = CreateSut();
        var decision = await sut.ValidateModelAccessAsync(Guid.NewGuid(), CancellationToken.None);

        decision.IsAllowed.Should().BeFalse();
        decision.DenialReason.Should().Contain("not found");
    }

    [Fact]
    public async Task ValidateModelAccess_InternalOnlyPolicy_ExternalModel_ShouldDeny()
    {
        SetupUser();
        var externalModel = CreateModel("gpt-4o", isInternal: false);
        _modelRepo.GetByIdAsync(externalModel.Id, Arg.Any<CancellationToken>()).Returns(externalModel);

        var policy = CreatePolicy("user", "user-1", internalOnly: true);
        SetupPolicies(policy);

        var sut = CreateSut();
        var decision = await sut.ValidateModelAccessAsync(externalModel.Id.Value, CancellationToken.None);

        decision.IsAllowed.Should().BeFalse();
        decision.DenialReason.Should().Contain("internal");
    }

    [Fact]
    public async Task ValidateModelAccess_BlockedModel_ShouldDeny()
    {
        SetupUser();
        var model = CreateModel("blocked-model", isInternal: true);
        _modelRepo.GetByIdAsync(model.Id, Arg.Any<CancellationToken>()).Returns(model);

        var policy = CreatePolicy("user", "user-1");
        policy.SetBlockedModels(model.Id.Value.ToString());
        SetupPolicies(policy);

        var sut = CreateSut();
        var decision = await sut.ValidateModelAccessAsync(model.Id.Value, CancellationToken.None);

        decision.IsAllowed.Should().BeFalse();
        decision.DenialReason.Should().Contain("blocked");
    }

    [Fact]
    public async Task ValidateModelAccess_NotInAllowedList_ShouldDeny()
    {
        SetupUser();
        var model = CreateModel("model-x", isInternal: true);
        var allowedModel = CreateModel("model-y", isInternal: true);
        _modelRepo.GetByIdAsync(model.Id, Arg.Any<CancellationToken>()).Returns(model);

        var policy = CreatePolicy("user", "user-1");
        policy.SetAllowedModels(allowedModel.Id.Value.ToString());
        SetupPolicies(policy);

        var sut = CreateSut();
        var decision = await sut.ValidateModelAccessAsync(model.Id.Value, CancellationToken.None);

        decision.IsAllowed.Should().BeFalse();
        decision.DenialReason.Should().Contain("allowed");
    }

    [Fact]
    public async Task ValidateModelAccess_InAllowedList_ShouldAllow()
    {
        SetupUser();
        var model = CreateModel("model-ok", isInternal: true);
        _modelRepo.GetByIdAsync(model.Id, Arg.Any<CancellationToken>()).Returns(model);

        var policy = CreatePolicy("user", "user-1");
        policy.SetAllowedModels(model.Id.Value.ToString());
        SetupPolicies(policy);

        var sut = CreateSut();
        var decision = await sut.ValidateModelAccessAsync(model.Id.Value, CancellationToken.None);

        decision.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateModelAccess_InactiveModel_ShouldDeny()
    {
        SetupUser();
        var model = CreateModel("deprecated-model", isInternal: true);
        model.Deprecate();
        _modelRepo.GetByIdAsync(model.Id, Arg.Any<CancellationToken>()).Returns(model);
        SetupPolicies();

        var sut = CreateSut();
        var decision = await sut.ValidateModelAccessAsync(model.Id.Value, CancellationToken.None);

        decision.IsAllowed.Should().BeFalse();
        decision.DenialReason.Should().Contain("not active");
    }
}
