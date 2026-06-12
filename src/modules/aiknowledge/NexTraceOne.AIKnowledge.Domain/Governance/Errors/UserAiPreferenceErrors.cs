using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Errors;

/// <summary>
/// Erros de domínio relacionados a preferências de IA do usuário.
/// </summary>
public static class UserAiPreferenceErrors
{
    public static Error NotFound(Guid id)
        => Error.NotFound(
            "UserAiPreference.NotFound",
            "Preferência de IA '{Id}' não encontrada.",
            id);

    public static Error NotFoundForFeature(string featureKey)
        => Error.NotFound(
            "UserAiPreference.NotFoundForFeature",
            "Nenhuma preferência de IA encontrada para a funcionalidade '{FeatureKey}'.",
            featureKey);

    public static Error Duplicate(Guid userId, string featureKey)
        => Error.Conflict(
            "UserAiPreference.Duplicate",
            "Já existe uma preferência de IA para o usuário '{UserId}' e funcionalidade '{FeatureKey}'.",
            userId, featureKey);

    public static Error InvalidModel(Guid modelId)
        => Error.Validation(
            "UserAiPreference.InvalidModel",
            "O modelo de IA '{ModelId}' não é válido ou não está disponível.",
            modelId);

    public static Error InvalidProvider(string providerId)
        => Error.Validation(
            "UserAiPreference.InvalidProvider",
            "O provider de IA '{ProviderId}' não é válido ou não está registrado.",
            providerId);
}
