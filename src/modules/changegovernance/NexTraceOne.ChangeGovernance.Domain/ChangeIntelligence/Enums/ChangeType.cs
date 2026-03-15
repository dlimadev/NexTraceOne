namespace NexTraceOne.ChangeIntelligence.Domain.Enums;

/// <summary>
/// Tipo de mudança no NexTraceOne.
/// Classifica a natureza da alteração para análise de impacto e confiança.
/// </summary>
public enum ChangeType
{
    /// <summary>Deployment de nova versão do serviço.</summary>
    Deployment = 0,

    /// <summary>Alteração de configuração.</summary>
    ConfigurationChange = 1,

    /// <summary>Alteração de contrato de API.</summary>
    ContractChange = 2,

    /// <summary>Alteração de schema ou payload.</summary>
    SchemaChange = 3,

    /// <summary>Alteração de dependência.</summary>
    DependencyChange = 4,

    /// <summary>Alteração de política ou regra de governança.</summary>
    PolicyChange = 5,

    /// <summary>Alteração operacional (runbook, infra, etc.).</summary>
    OperationalChange = 6
}
