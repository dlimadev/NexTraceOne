using NexTraceOne.AIKnowledge.Application.Governance.Features.SendAssistantMessage;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Services;

/// <summary>
/// Resultado da resolução de contexto de grounding para uma query de assistente.
/// Contém o contexto já aumentado com retrieval e os metadados associados.
/// </summary>
public sealed record GroundingResolutionResult(
    string GroundingContext,
    string SystemPrompt,
    IReadOnlyList<string> GroundingSources,
    string? ContextSummary,
    IReadOnlyList<string>? SuggestedSteps,
    IReadOnlyList<string>? Caveats,
    string ContextStrength,
    string ConfidenceLevel,
    string SourceWeightingSummary,
    AIUseCaseType UseCaseType);

/// <summary>
/// Serviço responsável por resolver o contexto de grounding para uma query de assistente.
/// Encapsula: classificação de use case, resolução de fontes, construção de contexto,
/// augmentação via retrieval services (docs, DB, telemetria) e construção do system prompt.
/// </summary>
public interface IContextGroundingService
{
    /// <summary>
    /// Resolve o contexto de grounding completo para uma query.
    /// Classifica o use case, resolve fontes, constrói contexto e augmenta com retrieval.
    /// </summary>
    Task<GroundingResolutionResult> ResolveGroundingAsync(
        string query,
        string persona,
        string? contextScope,
        Guid? serviceId,
        Guid? contractId,
        Guid? incidentId,
        Guid? changeId,
        Guid? teamId,
        Guid? domainId,
        string? contextBundleJson,
        IReadOnlyList<AIKnowledgeSource> availableSources,
        CancellationToken cancellationToken = default);
}
