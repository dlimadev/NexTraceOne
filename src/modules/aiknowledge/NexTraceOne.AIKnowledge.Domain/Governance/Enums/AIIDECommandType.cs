namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Tipos de comando disponíveis para clientes IDE.
/// Define as ações que podem ser executadas a partir de extensões VS Code e Visual Studio.
/// </summary>
public enum AIIDECommandType
{
    /// <summary>Conversa livre com o assistente de IA.</summary>
    Chat,

    /// <summary>Consulta de serviço por nome ou identificador.</summary>
    ServiceLookup,

    /// <summary>Consulta de contrato por endpoint ou tópico.</summary>
    ContractLookup,

    /// <summary>Geração de rascunho de contrato.</summary>
    ContractGenerate,

    /// <summary>Validação de compatibilidade de contrato.</summary>
    ContractValidate,

    /// <summary>Consulta de incidente relacionado a um serviço.</summary>
    IncidentLookup,

    /// <summary>Consulta de alterações recentes de um serviço.</summary>
    ChangeLookup,

    /// <summary>Consulta de runbook operacional.</summary>
    RunbookLookup,

    /// <summary>Resumo de contexto de um serviço (owner, contratos, dependências).</summary>
    ServiceSummary,

    /// <summary>Consulta genérica ao Source of Truth.</summary>
    SourceOfTruthQuery
}
