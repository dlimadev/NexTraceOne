namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Tipo de passo num plano de execução de agent.
/// Classifica a acção efectuada em cada etapa do plano agentic.
/// </summary>
public enum AgentStepType
{
    ContractLookup = 0,
    IncidentCorrelation = 1,
    DraftGeneration = 2,
    RunbookProposal = 3,
    AlertTriage = 4,
    ExternalSearch = 5,
}
