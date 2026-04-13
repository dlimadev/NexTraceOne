using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Services;

/// <summary>
/// Resultado da resolução de roteamento de modelo para uma requisição de assistente.
/// Contém modelo, provider, caminho e justificativa.
/// </summary>
public sealed record RoutingResolutionResult(
    string SelectedModel,
    string SelectedProvider,
    bool IsInternal,
    AIRoutingPath RoutingPath,
    string RoutingRationale,
    string CostClass,
    string EscalationReason,
    AIRoutingStrategy? AppliedStrategy);

/// <summary>
/// Serviço responsável por resolver o modelo e provider a usar para uma query,
/// baseado na persona, use case, routing strategies e model catalog.
/// Encapsula toda a lógica de seleção de modelo e construção da justificativa.
/// </summary>
public interface IAiRoutingResolver
{
    /// <summary>
    /// Resolve o modelo e provider mais adequados para a query,
    /// considerando routing strategies activas, persona e use case.
    /// </summary>
    Task<RoutingResolutionResult> ResolveRoutingAsync(
        string persona,
        AIUseCaseType useCaseType,
        string clientType,
        Guid? preferredModelId,
        string confidenceLevel,
        IReadOnlyList<AIRoutingStrategy> activeStrategies,
        CancellationToken cancellationToken = default);
}
