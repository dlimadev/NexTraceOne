using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Serviço de autorização de modelos de IA.
/// Avalia as políticas de acesso (AIAccessPolicy) para determinar quais modelos
/// um utilizador pode acessar, considerando scope (user, role, persona, team),
/// restrições de modelo interno/externo e listas de modelos permitidos/bloqueados.
/// </summary>
public interface IAiModelAuthorizationService
{
    /// <summary>
    /// Retorna os modelos disponíveis para o utilizador atual, filtrados pelas políticas aplicáveis.
    /// </summary>
    Task<ModelAuthorizationResult> GetAvailableModelsAsync(CancellationToken ct);

    /// <summary>
    /// Valida se o utilizador atual pode usar um modelo específico.
    /// Retorna resultado com sucesso ou razão da negação.
    /// </summary>
    Task<ModelAccessDecision> ValidateModelAccessAsync(Guid modelId, CancellationToken ct);
}

/// <summary>Resultado da avaliação de modelos disponíveis para o utilizador.</summary>
public sealed record ModelAuthorizationResult(
    IReadOnlyList<AuthorizedModel> Models,
    bool AllowExternalModels,
    string? AppliedPolicyName);

/// <summary>Modelo autorizado para o utilizador com metadados de classificação.</summary>
public sealed record AuthorizedModel(
    Guid ModelId,
    string Name,
    string DisplayName,
    string Provider,
    string ModelType,
    bool IsInternal,
    bool IsExternal,
    string Status,
    string Capabilities,
    bool IsDefault,
    string? Slug,
    int? ContextWindow);

/// <summary>Decisão de acesso a um modelo específico.</summary>
public sealed record ModelAccessDecision(
    bool IsAllowed,
    string? DenialReason,
    string? AppliedPolicyName,
    bool ModelIsInternal);
