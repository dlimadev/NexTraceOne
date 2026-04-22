namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Intenção classificada de um prompt de IA.
/// Utilizada pelo classificador NLP para selecionar o modelo de roteamento adequado.
/// GeneralQuery é o fallback quando nenhuma intenção específica é identificada.
/// </summary>
public enum PromptIntent
{
    CodeGeneration = 0,
    DocumentSummarization = 1,
    IncidentAnalysis = 2,
    ContractDraft = 3,
    ComplianceCheck = 4,
    GeneralQuery = 5,
}
