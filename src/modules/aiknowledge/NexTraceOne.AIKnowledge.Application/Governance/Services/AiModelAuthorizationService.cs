using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Application.Governance.Services;

/// <summary>
/// Implementação do serviço de autorização de modelos de IA.
/// Avalia as políticas de acesso (AIAccessPolicy) por prioridade de escopo:
/// user → role → persona → team → global.
/// A primeira política ativa encontrada para o utilizador determina o acesso.
/// Se nenhuma política se aplica, todos os modelos ativos ficam disponíveis (sem restrição).
/// </summary>
public sealed class AiModelAuthorizationService(
    IAiAccessPolicyRepository policyRepository,
    IAiModelRepository modelRepository,
    ICurrentUser currentUser,
    ILogger<AiModelAuthorizationService> logger) : IAiModelAuthorizationService
{
    public async Task<ModelAuthorizationResult> GetAvailableModelsAsync(CancellationToken ct)
    {
        var allModels = await modelRepository.ListAsync(
            provider: null,
            modelType: null,
            status: ModelStatus.Active,
            isInternal: null,
            ct);

        var applicablePolicy = await FindApplicablePolicyAsync(ct);

        if (applicablePolicy is null)
        {
            logger.LogDebug("No access policy found for user {UserId}; all active models available", currentUser.Id);
            return new ModelAuthorizationResult(
                MapModels(allModels),
                AllowExternalModels: true,
                AppliedPolicyName: null);
        }

        var filteredModels = FilterModelsByPolicy(allModels, applicablePolicy);

        logger.LogInformation(
            "Applied policy '{PolicyName}' for user {UserId}: {Count} models available, ExternalAllowed={AllowExternal}",
            applicablePolicy.Name, currentUser.Id, filteredModels.Count, applicablePolicy.AllowExternalAI);

        return new ModelAuthorizationResult(
            filteredModels,
            applicablePolicy.AllowExternalAI,
            applicablePolicy.Name);
    }

    public async Task<ModelAccessDecision> ValidateModelAccessAsync(Guid modelId, CancellationToken ct)
    {
        var model = await modelRepository.GetByIdAsync(AIModelId.From(modelId), ct);
        if (model is null)
            return new ModelAccessDecision(false, "Model not found", null, false);

        if (model.Status != ModelStatus.Active)
            return new ModelAccessDecision(false, "Model is not active", null, model.IsInternal);

        var applicablePolicy = await FindApplicablePolicyAsync(ct);
        if (applicablePolicy is null)
            return new ModelAccessDecision(true, null, null, model.IsInternal);

        if (applicablePolicy.InternalOnly && model.IsExternal)
            return new ModelAccessDecision(false, "Policy restricts to internal models only", applicablePolicy.Name, model.IsInternal);

        if (!applicablePolicy.AllowExternalAI && model.IsExternal)
            return new ModelAccessDecision(false, "External AI is not allowed by policy", applicablePolicy.Name, model.IsInternal);

        var blockedIds = ParseModelIds(applicablePolicy.BlockedModelIds);
        if (blockedIds.Contains(modelId))
            return new ModelAccessDecision(false, "Model is blocked by policy", applicablePolicy.Name, model.IsInternal);

        var allowedIds = ParseModelIds(applicablePolicy.AllowedModelIds);
        if (allowedIds.Count > 0 && !allowedIds.Contains(modelId))
            return new ModelAccessDecision(false, "Model is not in the allowed list", applicablePolicy.Name, model.IsInternal);

        return new ModelAccessDecision(true, null, applicablePolicy.Name, model.IsInternal);
    }

    private async Task<AIAccessPolicy?> FindApplicablePolicyAsync(CancellationToken ct)
    {
        var allPolicies = await policyRepository.ListAsync(scope: null, isActive: true, ct);

        // Prioridade: user → role → persona → team → group
        var userId = currentUser.Id;
        var userEmail = currentUser.Email;

        var userPolicy = allPolicies.FirstOrDefault(p =>
            string.Equals(p.Scope, "user", StringComparison.OrdinalIgnoreCase) &&
            (string.Equals(p.ScopeValue, userId, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(p.ScopeValue, userEmail, StringComparison.OrdinalIgnoreCase)));

        if (userPolicy is not null) return userPolicy;

        // role-based: check if user has the role matching any role policy
        var rolePolicies = allPolicies
            .Where(p => string.Equals(p.Scope, "role", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var rolePolicy in rolePolicies)
        {
            if (currentUser.HasPermission(rolePolicy.ScopeValue))
                return rolePolicy;
        }

        // persona-based: match against common persona patterns
        var personaPolicies = allPolicies
            .Where(p => string.Equals(p.Scope, "persona", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (personaPolicies.Count > 0)
            return personaPolicies.FirstOrDefault();

        // team-based
        var teamPolicies = allPolicies
            .Where(p => string.Equals(p.Scope, "team", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (teamPolicies.Count > 0)
            return teamPolicies.FirstOrDefault();

        // group-based
        var groupPolicy = allPolicies
            .FirstOrDefault(p => string.Equals(p.Scope, "group", StringComparison.OrdinalIgnoreCase));

        return groupPolicy;
    }

    private static IReadOnlyList<AuthorizedModel> FilterModelsByPolicy(
        IReadOnlyList<AIModel> models,
        AIAccessPolicy policy)
    {
        var allowedIds = ParseModelIds(policy.AllowedModelIds);
        var blockedIds = ParseModelIds(policy.BlockedModelIds);

        var filtered = models.Where(m =>
        {
            if (blockedIds.Contains(m.Id.Value))
                return false;

            if (allowedIds.Count > 0 && !allowedIds.Contains(m.Id.Value))
                return false;

            if (policy.InternalOnly && m.IsExternal)
                return false;

            if (!policy.AllowExternalAI && m.IsExternal)
                return false;

            return true;
        });

        return MapModels(filtered.ToList());
    }

    private static IReadOnlyList<AuthorizedModel> MapModels(IReadOnlyList<AIModel> models)
    {
        return models.Select(m => new AuthorizedModel(
            m.Id.Value,
            m.Name,
            m.DisplayName,
            m.Provider,
            m.ModelType.ToString(),
            m.IsInternal,
            m.IsExternal,
            m.Status.ToString(),
            m.Capabilities,
            m.IsDefaultForChat || m.IsDefaultForReasoning || m.IsDefaultForEmbeddings,
            m.Slug,
            m.ContextWindow)).ToList();
    }

    private static HashSet<Guid> ParseModelIds(string commaSeparatedIds)
    {
        if (string.IsNullOrWhiteSpace(commaSeparatedIds))
            return [];

        var ids = new HashSet<Guid>();
        foreach (var part in commaSeparatedIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Guid.TryParse(part, out var parsed))
                ids.Add(parsed);
        }
        return ids;
    }
}
