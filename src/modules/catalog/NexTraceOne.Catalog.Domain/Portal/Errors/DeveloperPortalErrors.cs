using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.DeveloperPortal.Domain.Errors;

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
}
