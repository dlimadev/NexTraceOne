using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.AiGovernance.Domain.Enums;

namespace NexTraceOne.AiGovernance.Domain.Entities;

/// <summary>
/// Representa o plano de execução construído para uma consulta de IA.
/// Consolida a decisão de roteamento, fontes selecionadas, pesos,
/// enriquecimento de contexto e metadados de governança.
///
/// Invariantes:
/// - CorrelationId é obrigatório para rastreamento.
/// - Entidade imutável após criação.
/// </summary>
public sealed class AIExecutionPlan : AuditableEntity<AIExecutionPlanId>
{
    private AIExecutionPlan() { }

    /// <summary>Identificador de correlação para rastreamento fim-a-fim.</summary>
    public string CorrelationId { get; private set; } = string.Empty;

    /// <summary>Query original do utilizador.</summary>
    public string InputQuery { get; private set; } = string.Empty;

    /// <summary>Persona do utilizador.</summary>
    public string Persona { get; private set; } = string.Empty;

    /// <summary>Caso de uso classificado.</summary>
    public AIUseCaseType UseCaseType { get; private set; }

    /// <summary>Modelo selecionado para execução.</summary>
    public string SelectedModel { get; private set; } = string.Empty;

    /// <summary>Provedor do modelo selecionado.</summary>
    public string SelectedProvider { get; private set; } = string.Empty;

    /// <summary>Indica se a execução é interna.</summary>
    public bool IsInternal { get; private set; }

    /// <summary>Caminho de roteamento selecionado.</summary>
    public AIRoutingPath RoutingPath { get; private set; }

    /// <summary>Fontes selecionadas para enriquecimento, separadas por vírgula.</summary>
    public string SelectedSources { get; private set; } = string.Empty;

    /// <summary>Resumo dos pesos das fontes.</summary>
    public string SourceWeightingSummary { get; private set; } = string.Empty;

    /// <summary>Decisão de política aplicada.</summary>
    public string PolicyDecision { get; private set; } = string.Empty;

    /// <summary>Classe de custo estimado.</summary>
    public string EstimatedCostClass { get; private set; } = string.Empty;

    /// <summary>Justificativa consolidada do plano.</summary>
    public string RationaleSummary { get; private set; } = string.Empty;

    /// <summary>Nível de confiança estimado.</summary>
    public AIConfidenceLevel ConfidenceLevel { get; private set; }

    /// <summary>Razão de escalonamento se aplicável.</summary>
    public AIEscalationReason EscalationReason { get; private set; }

    /// <summary>Data/hora UTC da criação do plano.</summary>
    public DateTimeOffset PlannedAt { get; private set; }

    /// <summary>
    /// Cria um novo plano de execução com metadados completos.
    /// </summary>
    public static AIExecutionPlan Create(
        string correlationId,
        string inputQuery,
        string persona,
        AIUseCaseType useCaseType,
        string selectedModel,
        string selectedProvider,
        bool isInternal,
        AIRoutingPath routingPath,
        string selectedSources,
        string sourceWeightingSummary,
        string policyDecision,
        string estimatedCostClass,
        string rationaleSummary,
        AIConfidenceLevel confidenceLevel,
        AIEscalationReason escalationReason,
        DateTimeOffset plannedAt)
    {
        Guard.Against.NullOrWhiteSpace(correlationId);
        Guard.Against.NullOrWhiteSpace(inputQuery);
        Guard.Against.NullOrWhiteSpace(persona);
        Guard.Against.NullOrWhiteSpace(selectedModel);
        Guard.Against.NullOrWhiteSpace(selectedProvider);

        return new AIExecutionPlan
        {
            Id = AIExecutionPlanId.New(),
            CorrelationId = correlationId,
            InputQuery = inputQuery,
            Persona = persona,
            UseCaseType = useCaseType,
            SelectedModel = selectedModel,
            SelectedProvider = selectedProvider,
            IsInternal = isInternal,
            RoutingPath = routingPath,
            SelectedSources = selectedSources ?? string.Empty,
            SourceWeightingSummary = sourceWeightingSummary ?? string.Empty,
            PolicyDecision = policyDecision ?? string.Empty,
            EstimatedCostClass = estimatedCostClass ?? string.Empty,
            RationaleSummary = rationaleSummary ?? string.Empty,
            ConfidenceLevel = confidenceLevel,
            EscalationReason = escalationReason,
            PlannedAt = plannedAt
        };
    }
}

/// <summary>Identificador fortemente tipado de AIExecutionPlan.</summary>
public sealed record AIExecutionPlanId(Guid Value) : TypedIdBase(Value)
{
    public static AIExecutionPlanId New() => new(Guid.NewGuid());
    public static AIExecutionPlanId From(Guid id) => new(id);
}
