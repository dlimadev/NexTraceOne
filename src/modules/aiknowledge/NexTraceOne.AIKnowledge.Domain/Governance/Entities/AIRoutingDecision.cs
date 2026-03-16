using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.AiGovernance.Domain.Enums;

namespace NexTraceOne.AiGovernance.Domain.Entities;

/// <summary>
/// Regista a decisão de roteamento tomada para uma execução específica de IA.
/// Captura o modelo selecionado, caminho escolhido, justificativa e metadados
/// de política para auditoria e explicabilidade completa.
///
/// Invariantes:
/// - CorrelationId é obrigatório para rastreamento fim-a-fim.
/// - Entidade imutável após criação — decisão não pode ser alterada.
/// </summary>
public sealed class AIRoutingDecision : AuditableEntity<AIRoutingDecisionId>
{
    private AIRoutingDecision() { }

    /// <summary>Identificador de correlação com a execução de IA.</summary>
    public string CorrelationId { get; private set; } = string.Empty;

    /// <summary>Persona do utilizador na decisão.</summary>
    public string Persona { get; private set; } = string.Empty;

    /// <summary>Caso de uso classificado para a consulta.</summary>
    public AIUseCaseType UseCaseType { get; private set; }

    /// <summary>Tipo de cliente que originou a consulta.</summary>
    public string ClientType { get; private set; } = string.Empty;

    /// <summary>Caminho de roteamento selecionado.</summary>
    public AIRoutingPath SelectedPath { get; private set; }

    /// <summary>Nome do modelo selecionado para execução.</summary>
    public string SelectedModelName { get; private set; } = string.Empty;

    /// <summary>Provedor do modelo selecionado.</summary>
    public string SelectedProvider { get; private set; } = string.Empty;

    /// <summary>Indica se o modelo selecionado é interno.</summary>
    public bool IsInternalModel { get; private set; }

    /// <summary>Identificador da estratégia aplicada (pode ser nulo se padrão).</summary>
    public Guid? AppliedStrategyId { get; private set; }

    /// <summary>Nome da política aplicada na decisão.</summary>
    public string? AppliedPolicyName { get; private set; }

    /// <summary>Razão de escalonamento (None se sem escalonamento).</summary>
    public AIEscalationReason EscalationReason { get; private set; }

    /// <summary>Justificativa legível da decisão de roteamento.</summary>
    public string Rationale { get; private set; } = string.Empty;

    /// <summary>Classe de custo estimado (ex: "low", "medium", "high").</summary>
    public string EstimatedCostClass { get; private set; } = string.Empty;

    /// <summary>Nível de confiança estimado para a resposta.</summary>
    public AIConfidenceLevel ConfidenceLevel { get; private set; }

    /// <summary>Fontes selecionadas para grounding, separadas por vírgula.</summary>
    public string SelectedSources { get; private set; } = string.Empty;

    /// <summary>Resumo dos pesos das fontes selecionadas.</summary>
    public string SourceWeightingSummary { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC da decisão.</summary>
    public DateTimeOffset DecidedAt { get; private set; }

    /// <summary>
    /// Regista uma nova decisão de roteamento com metadados completos.
    /// </summary>
    public static AIRoutingDecision Record(
        string correlationId,
        string persona,
        AIUseCaseType useCaseType,
        string clientType,
        AIRoutingPath selectedPath,
        string selectedModelName,
        string selectedProvider,
        bool isInternalModel,
        Guid? appliedStrategyId,
        string? appliedPolicyName,
        AIEscalationReason escalationReason,
        string rationale,
        string estimatedCostClass,
        AIConfidenceLevel confidenceLevel,
        string selectedSources,
        string sourceWeightingSummary,
        DateTimeOffset decidedAt)
    {
        Guard.Against.NullOrWhiteSpace(correlationId);
        Guard.Against.NullOrWhiteSpace(persona);
        Guard.Against.NullOrWhiteSpace(clientType);
        Guard.Against.NullOrWhiteSpace(selectedModelName);
        Guard.Against.NullOrWhiteSpace(selectedProvider);
        Guard.Against.NullOrWhiteSpace(rationale);

        return new AIRoutingDecision
        {
            Id = AIRoutingDecisionId.New(),
            CorrelationId = correlationId,
            Persona = persona,
            UseCaseType = useCaseType,
            ClientType = clientType,
            SelectedPath = selectedPath,
            SelectedModelName = selectedModelName,
            SelectedProvider = selectedProvider,
            IsInternalModel = isInternalModel,
            AppliedStrategyId = appliedStrategyId,
            AppliedPolicyName = appliedPolicyName,
            EscalationReason = escalationReason,
            Rationale = rationale,
            EstimatedCostClass = estimatedCostClass,
            ConfidenceLevel = confidenceLevel,
            SelectedSources = selectedSources,
            SourceWeightingSummary = sourceWeightingSummary,
            DecidedAt = decidedAt
        };
    }
}

/// <summary>Identificador fortemente tipado de AIRoutingDecision.</summary>
public sealed record AIRoutingDecisionId(Guid Value) : TypedIdBase(Value)
{
    public static AIRoutingDecisionId New() => new(Guid.NewGuid());
    public static AIRoutingDecisionId From(Guid id) => new(id);
}
