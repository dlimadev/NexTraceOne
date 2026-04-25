using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Executa uma AiSkill através do AI Runtime (LLM).
/// Constrói o prompt a partir do SkillContent, injeta contexto e executa via provider.
/// </summary>
public interface ISkillExecutor
{
    /// <summary>
    /// Executa a skill com os inputs fornecidos e retorna o output estruturado.
    /// </summary>
    Task<SkillExecutionOutput> ExecuteAsync(
        AiSkill skill,
        string inputJson,
        string? modelOverride,
        Guid tenantId,
        string executedBy,
        CancellationToken ct = default);
}

/// <summary>Resultado da execução de uma skill via LLM.</summary>
public sealed record SkillExecutionOutput(
    bool Success,
    string OutputJson,
    string ModelUsed,
    string ProviderId,
    int PromptTokens,
    int CompletionTokens,
    TimeSpan Duration,
    string? ErrorMessage = null);
