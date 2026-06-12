using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Vinculação de funcionalidade da plataforma a um modelo de IA específico.
/// Permite governança granular definindo qual modelo deve ser utilizado
/// para cada feature (ex: "catalog.contract-draft" → claude-sonnet-4-6).
///
/// Invariantes:
/// - FeatureKey identifica de forma única a funcionalidade, em lowercase.
/// - RequiredModelId e RequiredModelName são obrigatórios.
/// - TenantId é obrigatório — vinculação é sempre por tenant.
/// - FallbackModelId deve diferir de RequiredModelId.
/// </summary>
public sealed class AiFeatureModelBinding : AuditableEntity<AiFeatureModelBindingId>
{
    private AiFeatureModelBinding() { }

    /// <summary>Chave única da funcionalidade (ex: "catalog.contract-draft", "aiknowledge.assistant-chat").</summary>
    public string FeatureKey { get; private set; } = string.Empty;

    /// <summary>Descrição da vinculação e justificativa da escolha de modelo.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Identificador do tenant proprietário desta vinculação.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>ID do modelo de IA obrigatório para esta funcionalidade.</summary>
    public Guid RequiredModelId { get; private set; }

    /// <summary>Nome do modelo obrigatório (desnormalizado para rastreabilidade).</summary>
    public string RequiredModelName { get; private set; } = string.Empty;

    /// <summary>Provider do modelo obrigatório (ex: "anthropic", "ollama").</summary>
    public string RequiredProviderId { get; private set; } = string.Empty;

    /// <summary>ID do modelo de fallback (opcional — usado se o modelo obrigatório estiver indisponível).</summary>
    public Guid? FallbackModelId { get; private set; }

    /// <summary>Nome do modelo de fallback (desnormalizado para rastreabilidade).</summary>
    public string? FallbackModelName { get; private set; }

    /// <summary>Provider do modelo de fallback.</summary>
    public string? FallbackProviderId { get; private set; }

    /// <summary>Modo de operação da vinculação: Disabled, Internal ou ExternalProduct.</summary>
    public AiBindingMode Mode { get; private set; }

    /// <summary>Indica se esta vinculação está ativa e sendo aplicada.</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Cria uma nova vinculação de funcionalidade a modelo de IA.
    /// A vinculação inicia ativa e pronta para ser aplicada.
    /// </summary>
    public static AiFeatureModelBinding Create(
        Guid tenantId,
        string featureKey,
        string description,
        Guid requiredModelId,
        string requiredModelName,
        string requiredProviderId,
        AiBindingMode mode = AiBindingMode.Internal)
    {
        Guard.Against.Default(tenantId, nameof(tenantId));
        Guard.Against.NullOrWhiteSpace(featureKey);
        Guard.Against.Default(requiredModelId, nameof(requiredModelId));
        Guard.Against.NullOrWhiteSpace(requiredModelName);
        Guard.Against.NullOrWhiteSpace(requiredProviderId);

        return new AiFeatureModelBinding
        {
            Id = AiFeatureModelBindingId.New(),
            TenantId = tenantId,
            FeatureKey = featureKey.Trim().ToLowerInvariant(),
            Description = description ?? string.Empty,
            RequiredModelId = requiredModelId,
            RequiredModelName = requiredModelName,
            RequiredProviderId = requiredProviderId,
            Mode = mode,
            IsActive = true
        };
    }

    /// <summary>
    /// Atualiza a descrição, o modo e o modelo obrigatório da vinculação.
    /// </summary>
    public Result<Unit> Update(
        string description,
        Guid requiredModelId,
        string requiredModelName,
        string requiredProviderId,
        AiBindingMode? mode = null)
    {
        Guard.Against.Default(requiredModelId, nameof(requiredModelId));
        Guard.Against.NullOrWhiteSpace(requiredModelName);
        Guard.Against.NullOrWhiteSpace(requiredProviderId);

        Description = description ?? string.Empty;
        RequiredModelId = requiredModelId;
        RequiredModelName = requiredModelName;
        RequiredProviderId = requiredProviderId;
        if (mode.HasValue)
            Mode = mode.Value;
        return Unit.Value;
    }

    /// <summary>
    /// Define o modo de operação da vinculação.
    /// </summary>
    public Result<Unit> SetMode(AiBindingMode mode)
    {
        Mode = mode;
        return Unit.Value;
    }

    /// <summary>
    /// Define o modelo de fallback para esta vinculação.
    /// O fallback é ativado automaticamente se o modelo obrigatório estiver indisponível.
    /// </summary>
    public Result<Unit> SetFallback(
        Guid fallbackModelId,
        string fallbackModelName,
        string fallbackProviderId)
    {
        Guard.Against.Default(fallbackModelId, nameof(fallbackModelId));
        Guard.Against.NullOrWhiteSpace(fallbackModelName);
        Guard.Against.NullOrWhiteSpace(fallbackProviderId);

        if (fallbackModelId == RequiredModelId)
            return Error.Validation(
                "AiFeatureModelBinding.SameFallback",
                "O modelo de fallback não pode ser igual ao modelo obrigatório.");

        FallbackModelId = fallbackModelId;
        FallbackModelName = fallbackModelName;
        FallbackProviderId = fallbackProviderId;
        return Unit.Value;
    }

    /// <summary>Remove o modelo de fallback desta vinculação.</summary>
    public Result<Unit> ClearFallback()
    {
        FallbackModelId = null;
        FallbackModelName = null;
        FallbackProviderId = null;
        return Unit.Value;
    }

    /// <summary>Ativa a vinculação. Operação idempotente.</summary>
    public Result<Unit> Activate()
    {
        IsActive = true;
        return Unit.Value;
    }

    /// <summary>Desativa a vinculação. Operação idempotente.</summary>
    public Result<Unit> Deactivate()
    {
        IsActive = false;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de AiFeatureModelBinding.</summary>
public sealed record AiFeatureModelBindingId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AiFeatureModelBindingId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AiFeatureModelBindingId From(Guid id) => new(id);
}
