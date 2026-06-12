using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Preferência de IA definida por um usuário para uma funcionalidade específica da plataforma.
/// Permite ao usuário escolher: não usar IA, usar IA interna (com modelo específico),
/// ou usar um produto de IA externo (ChatGPT, Claude, Gemini, Copilot).
///
/// Invariantes:
/// - FeatureKey identifica a funcionalidade; "*" representa preferência global do usuário.
/// - TenantId é obrigatório — preferências são sempre por tenant.
/// - Quando PreferenceType = Disabled, não deve haver modelo nem produto externo configurado.
/// - Quando PreferenceType = Internal, PreferredModelId deve ser informado.
/// - Quando PreferenceType = ExternalProduct, ExternalProduct deve ser informado.
/// </summary>
public sealed class UserAiPreference : AuditableEntity<UserAiPreferenceId>
{
    private UserAiPreference() { }

    /// <summary>Identificador do usuário dono da preferência.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Identificador do tenant proprietário desta preferência.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Chave da funcionalidade (ex: "catalog.contract-draft"); "*" para global.</summary>
    public string FeatureKey { get; private set; } = string.Empty;

    /// <summary>Tipo de preferência: Disabled, Internal ou ExternalProduct.</summary>
    public AiPreferenceType PreferenceType { get; private set; }

    /// <summary>ID do modelo interno preferido (quando PreferenceType = Internal).</summary>
    public Guid? PreferredModelId { get; private set; }

    /// <summary>ID do provider interno preferido (quando PreferenceType = Internal).</summary>
    public string? PreferredProviderId { get; private set; }

    /// <summary>Produto de IA externo (quando PreferenceType = ExternalProduct).</summary>
    public ExternalAiProductType? ExternalProduct { get; private set; }

    /// <summary>Modelo específico do produto externo (ex: "gpt-4o").</summary>
    public string? ExternalProductModel { get; private set; }

    /// <summary>Motivo opcional de desabilitação (para analytics).</summary>
    public string? DisableReason { get; private set; }

    /// <summary>Indica se esta preferência está ativa.</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Cria uma nova preferência de IA para um usuário.
    /// </summary>
    public static Result<UserAiPreference> Create(
        Guid userId,
        Guid tenantId,
        string featureKey,
        AiPreferenceType preferenceType,
        Guid? preferredModelId = null,
        string? preferredProviderId = null,
        ExternalAiProductType? externalProduct = null,
        string? externalProductModel = null,
        string? disableReason = null)
    {
        Guard.Against.Default(userId, nameof(userId));
        Guard.Against.Default(tenantId, nameof(tenantId));
        Guard.Against.NullOrWhiteSpace(featureKey);

        var preference = new UserAiPreference
        {
            Id = UserAiPreferenceId.New(),
            UserId = userId,
            TenantId = tenantId,
            FeatureKey = featureKey.Trim().ToLowerInvariant(),
            PreferenceType = preferenceType,
            PreferredModelId = preferredModelId,
            PreferredProviderId = preferredProviderId,
            ExternalProduct = externalProduct,
            ExternalProductModel = externalProductModel,
            DisableReason = disableReason,
            IsActive = true
        };

        var validation = preference.Validate();
        if (validation.IsFailure)
            return validation.Error;

        return preference;
    }

    /// <summary>
    /// Atualiza a preferência para usar IA interna com modelo específico.
    /// </summary>
    public Result<Unit> SetInternalPreference(Guid modelId, string providerId)
    {
        Guard.Against.Default(modelId, nameof(modelId));
        Guard.Against.NullOrWhiteSpace(providerId);

        PreferenceType = AiPreferenceType.Internal;
        PreferredModelId = modelId;
        PreferredProviderId = providerId;
        ExternalProduct = null;
        ExternalProductModel = null;
        DisableReason = null;

        return Unit.Value;
    }

    /// <summary>
    /// Atualiza a preferência para usar produto de IA externo.
    /// </summary>
    public Result<Unit> SetExternalProductPreference(ExternalAiProductType product, string? productModel = null)
    {
        PreferenceType = AiPreferenceType.ExternalProduct;
        ExternalProduct = product;
        ExternalProductModel = productModel;
        PreferredModelId = null;
        PreferredProviderId = null;
        DisableReason = null;

        return Unit.Value;
    }

    /// <summary>
    /// Atualiza a preferência para desabilitar IA.
    /// </summary>
    public Result<Unit> SetDisabledPreference(string? reason = null)
    {
        PreferenceType = AiPreferenceType.Disabled;
        PreferredModelId = null;
        PreferredProviderId = null;
        ExternalProduct = null;
        ExternalProductModel = null;
        DisableReason = reason;

        return Unit.Value;
    }

    /// <summary>Ativa a preferência. Operação idempotente.</summary>
    public Result<Unit> Activate()
    {
        IsActive = true;
        return Unit.Value;
    }

    /// <summary>Desativa a preferência. Operação idempotente.</summary>
    public Result<Unit> Deactivate()
    {
        IsActive = false;
        return Unit.Value;
    }

    private Result<Unit> Validate()
    {
        return PreferenceType switch
        {
            AiPreferenceType.Disabled when PreferredModelId.HasValue || ExternalProduct.HasValue
                => Error.Validation(
                    "UserAiPreference.InvalidDisabled",
                    "Preferência do tipo Disabled não deve ter modelo ou produto externo configurado."),

            AiPreferenceType.Internal when !PreferredModelId.HasValue
                => Error.Validation(
                    "UserAiPreference.InvalidInternal",
                    "Preferência do tipo Internal requer PreferredModelId."),

            AiPreferenceType.ExternalProduct when !ExternalProduct.HasValue
                => Error.Validation(
                    "UserAiPreference.InvalidExternal",
                    "Preferência do tipo ExternalProduct requer ExternalProduct."),

            _ => Unit.Value
        };
    }
}

/// <summary>Identificador fortemente tipado de UserAiPreference.</summary>
public sealed record UserAiPreferenceId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static UserAiPreferenceId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static UserAiPreferenceId From(Guid id) => new(id);
}
