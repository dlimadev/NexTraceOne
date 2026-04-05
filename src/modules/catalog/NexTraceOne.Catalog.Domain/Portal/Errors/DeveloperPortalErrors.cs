using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Catalog.Domain.Portal.Errors;

/// <summary>
/// Catálogo centralizado de erros do módulo DeveloperPortal com códigos i18n.
/// Cada erro possui código único para rastreabilidade em logs e documentação.
/// Padrão: DeveloperPortal.{Entidade}.{Descrição}
/// </summary>
public static class DeveloperPortalErrors
{
    /// <summary>Subscrição não encontrada.</summary>
    public static Error SubscriptionNotFound(string subscriptionId) =>
        Error.NotFound("DeveloperPortal.Subscription.NotFound", "Subscription '{0}' was not found.", subscriptionId);

    /// <summary>Já existe uma subscrição para esta combinação de API e subscritor.</summary>
    public static Error SubscriptionAlreadyExists(string apiAssetId, string subscriberId) =>
        Error.Conflict("DeveloperPortal.Subscription.AlreadyExists", "A subscription for API '{0}' by subscriber '{1}' already exists.", apiAssetId, subscriberId);

    /// <summary>A subscrição já está ativa.</summary>
    public static Error SubscriptionAlreadyActive(string subscriptionId) =>
        Error.Conflict("DeveloperPortal.Subscription.AlreadyActive", "Subscription '{0}' is already active.", subscriptionId);

    /// <summary>A subscrição já está inativa.</summary>
    public static Error SubscriptionAlreadyInactive(string subscriptionId) =>
        Error.Conflict("DeveloperPortal.Subscription.AlreadyInactive", "Subscription '{0}' is already inactive.", subscriptionId);

    /// <summary>Sessão de playground não encontrada.</summary>
    public static Error PlaygroundSessionNotFound(string sessionId) =>
        Error.NotFound("DeveloperPortal.PlaygroundSession.NotFound", "Playground session '{0}' was not found.", sessionId);

    /// <summary>Playground desativado para esta API.</summary>
    public static Error PlaygroundDisabledForApi(string apiAssetId) =>
        Error.Business("DeveloperPortal.PlaygroundSession.DisabledForApi", "Playground is disabled for API '{0}'.", apiAssetId);

    /// <summary>Geração de código não permitida para o utilizador ou API.</summary>
    public static Error CodeGenerationNotAllowed(string apiAssetId) =>
        Error.Forbidden("DeveloperPortal.CodeGeneration.NotAllowed", "Code generation is not allowed for API '{0}'.", apiAssetId);

    /// <summary>Contrato inválido ou incompatível para geração de código.</summary>
    public static Error InvalidContractForGeneration(string contractVersion) =>
        Error.Validation("DeveloperPortal.CodeGeneration.InvalidContract", "Contract version '{0}' is invalid or incompatible for code generation.", contractVersion);

    /// <summary>Pesquisa salva não encontrada.</summary>
    public static Error SavedSearchNotFound(string savedSearchId) =>
        Error.NotFound("DeveloperPortal.SavedSearch.NotFound", "Saved search '{0}' was not found.", savedSearchId);

    /// <summary>URL de webhook inválida ou ausente quando o canal é Webhook.</summary>
    public static Error InvalidWebhookUrl() =>
        Error.Validation("DeveloperPortal.Subscription.InvalidWebhookUrl", "A valid webhook URL is required when the notification channel is Webhook.");

    /// <summary>API não encontrada no catálogo.</summary>
    public static Error ApiNotFound(string apiAssetId) =>
        Error.NotFound("DeveloperPortal.Api.NotFound", "API '{0}' was not found in the catalog.", apiAssetId);

    // ── Publication Center ────────────────────────────────────────────────────

    /// <summary>Entrada de publicação não encontrada.</summary>
    public static Error PublicationEntryNotFound(string entryId) =>
        Error.NotFound("DeveloperPortal.Publication.NotFound", "Contract publication entry '{0}' was not found.", entryId);

    /// <summary>Já existe uma publicação ativa para esta versão de contrato.</summary>
    public static Error PublicationAlreadyExists(string contractVersionId) =>
        Error.Conflict("DeveloperPortal.Publication.AlreadyExists", "A publication entry for contract version '{0}' already exists.", contractVersionId);

    /// <summary>Transição de estado de publicação inválida.</summary>
    public static Error PublicationInvalidTransition(string from, string to) =>
        Error.Business("DeveloperPortal.Publication.InvalidTransition", "Cannot transition publication from '{0}' to '{1}'.", from, to);

    /// <summary>Versão de contrato não encontrada para publicação.</summary>
    public static Error ContractVersionNotFoundForPublication(string contractVersionId) =>
        Error.NotFound("DeveloperPortal.Publication.ContractVersionNotFound", "Contract version '{0}' was not found or is not in a publishable state.", contractVersionId);

    /// <summary>Versão de contrato não está num estado publicável (deve ser Approved ou Locked).</summary>
    public static Error ContractVersionNotPublishable(string contractVersionId, string lifecycleState) =>
        Error.Business("DeveloperPortal.Publication.NotPublishable", "Contract version '{0}' is in state '{1}' and cannot be published to the portal.", contractVersionId, lifecycleState);

    // ── API Keys ─────────────────────────────────────────────────────────────

    /// <summary>API Key não encontrada.</summary>
    public static Error ApiKeyNotFound(string id) =>
        Error.NotFound("API_KEY_NOT_FOUND", $"API key '{id}' not found.");

    /// <summary>API Key já foi revogada.</summary>
    public static Error ApiKeyAlreadyRevoked(string id) =>
        Error.Business("API_KEY_ALREADY_REVOKED", $"API key '{id}' is already revoked.");

    /// <summary>API Key expirada.</summary>
    public static Error ApiKeyExpired(string id) =>
        Error.Business("API_KEY_EXPIRED", $"API key '{id}' has expired.");

    // ── Rate Limit ────────────────────────────────────────────────────────────

    /// <summary>Política de rate limit não encontrada.</summary>
    public static Error RateLimitPolicyNotFound(Guid apiAssetId) =>
        Error.NotFound("RATE_LIMIT_NOT_FOUND", $"Rate limit policy for API '{apiAssetId}' not found.");

    /// <summary>Valores de rate limit inconsistentes.</summary>
    public static Error InvalidRateLimitValues() =>
        Error.Validation("INVALID_RATE_LIMITS", "Rate limit values are inconsistent: requestsPerDay must be greater than requestsPerHour, which must be greater than requestsPerMinute.");

    // ── Subscription Approval ─────────────────────────────────────────────────

    /// <summary>Subscrição não está pendente de aprovação.</summary>
    public static Error SubscriptionNotPendingApproval(string id) =>
        Error.Business("SUBSCRIPTION_NOT_PENDING", $"Subscription '{id}' is not pending approval.");

    /// <summary>Subscrição já foi rejeitada.</summary>
    public static Error SubscriptionAlreadyRejected(string id) =>
        Error.Business("SUBSCRIPTION_ALREADY_REJECTED", $"Subscription '{id}' is already rejected.");
}
