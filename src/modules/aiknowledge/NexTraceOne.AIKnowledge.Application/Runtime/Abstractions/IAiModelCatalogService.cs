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
