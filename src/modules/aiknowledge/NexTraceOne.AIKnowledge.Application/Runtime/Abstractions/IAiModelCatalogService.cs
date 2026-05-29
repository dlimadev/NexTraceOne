namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Serviço de catálogo de modelos. Resolve o modelo adequado para cada requisição
/// considerando finalidade, provider, políticas de acesso e disponibilidade.
/// </summary>
public interface IAiModelCatalogService
{
    /// <summary>Resolve o modelo default para uma finalidade (ex: "chat", "embedding").</summary>
    Task<ResolvedModel?> ResolveDefaultModelAsync(
        string purpose,
        CancellationToken cancellationToken = default);

    /// <summary>Resolve um modelo específico por ID.</summary>
    Task<ResolvedModel?> ResolveModelByIdAsync(
        Guid modelId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve o modelo para uma funcionalidade específica da plataforma.
    /// Consulta a vinculação de feature (AiFeatureModelBinding) do tenant;
    /// se não existir ou estiver inativa, usa o modelo default para a finalidade indicada.
    /// </summary>
    Task<ResolvedModel?> ResolveModelForFeatureAsync(
        string featureKey,
        string fallbackPurpose,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

/// <summary>Modelo resolvido com informação de provider.</summary>
public sealed record ResolvedModel(
    Guid ModelId,
    string ModelName,
    string DisplayName,
    string ProviderId,
    string ProviderDisplayName,
    bool IsInternal,
    string Capabilities,
    int? ContextWindow = null);
