using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Gateway central de execução de IA — único ponto de entrada para qualquer
/// consumo de IA na plataforma. Resolve automaticamente o provider, modelo,
/// e aplica governança (políticas, quotas, guardrails) antes de executar.
/// </summary>
public interface IAiExecutionGateway
{
    /// <summary>
    /// Executa uma requisição de IA resolvendo provider, modelo e governança automaticamente.
    /// </summary>
    Task<AiExecutionResult> ExecuteAsync(
        AiExecutionRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Executa chat completion streaming resolvendo provider automaticamente.
    /// </summary>
    IAsyncEnumerable<AiExecutionStreamChunk> ExecuteStreamingAsync(
        AiExecutionRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Resolve qual provider/modelo seria usado sem executar (para preview/UI).
    /// </summary>
    Task<AiExecutionPlan> PreviewExecutionAsync(
        AiExecutionRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Verifica se IA está disponível para uma feature específica para o usuário atual.
    /// </summary>
    Task<AiAvailabilityStatus> CheckAvailabilityAsync(
        string featureKey,
        CancellationToken ct = default);
}

/// <summary>
/// Requisição de execução de IA passada ao gateway.
/// </summary>
public sealed record AiExecutionRequest(
    string FeatureKey,
    string RequestType,
    string? UserPrompt = null,
    IReadOnlyList<ChatMessage>? Messages = null,
    string? SystemPrompt = null,
    IReadOnlyList<FunctionDefinition>? Tools = null,
    Guid? TargetModelId = null,
    Dictionary<string, object>? ContextData = null,
    bool AllowExternalProduct = true,
    bool AllowFallback = true,
    float? Temperature = null,
    int? MaxTokens = null);

/// <summary>
/// Resultado da execução de IA pelo gateway.
/// </summary>
public sealed record AiExecutionResult(
    bool Success,
    string? Content,
    AiProviderType ProviderType,
    string ResolvedProviderId,
    string ResolvedModelId,
    string ResolvedModelDisplayName,
    int PromptTokens,
    int CompletionTokens,
    TimeSpan Duration,
    Guid? RoutingDecisionId = null,
    bool WasFallbackUsed = false,
    string? ErrorMessage = null);

/// <summary>
/// Chunk de stream retornado pelo gateway.
/// </summary>
public sealed record AiExecutionStreamChunk(
    string? Content,
    bool IsComplete,
    AiProviderType ProviderType,
    string ResolvedProviderId,
    string ResolvedModelId,
    int PromptTokens = 0,
    int CompletionTokens = 0,
    string? ErrorMessage = null);

/// <summary>
/// Plano de execução preview — informa qual provider/modelo seria usado.
/// </summary>
public sealed record AiExecutionPlan(
    AiProviderType ProviderType,
    string ProviderId,
    string ModelId,
    string ModelDisplayName,
    bool IsAvailable,
    string? UnavailabilityReason,
    decimal? EstimatedCost,
    IReadOnlyList<string> AppliedPolicies);

/// <summary>
/// Status de disponibilidade de IA para uma funcionalidade.
/// </summary>
public enum AiAvailabilityStatus
{
    Available = 0,
    DisabledByUser = 1,
    DisabledByTenant = 2,
    DisabledByFeatureFlag = 3,
    DisabledByPolicy = 4,
    QuotaExceeded = 5,
    NoProviderAvailable = 6,
    GuardrailBlocked = 7,
    AccessDenied = 8
}

/// <summary>
/// Tipo de provider resolvido pelo gateway.
/// </summary>
public enum AiProviderType
{
    Null = 0,
    Internal = 1,
    ExternalProduct = 2
}
