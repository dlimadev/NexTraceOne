namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

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
    OperationalChange = 6,

    // ── Novos tipos de mudança para core systems / mainframe ──

    /// <summary>Alteração de batch job (JCL, scheduling, parâmetros).</summary>
    BatchJobChange = 7,

    /// <summary>Alteração de copybook COBOL (layout de dados).</summary>
    CopybookChange = 8,

    /// <summary>Alteração de transação CICS (programa, configuração).</summary>
    CicsTransactionChange = 9,

    /// <summary>Alteração de configuração MQ (filas, canais, queue manager).</summary>
    MqConfigurationChange = 10,

    /// <summary>Alteração de infraestrutura mainframe (LPAR, sysplex).</summary>
    MainframeInfraChange = 11,

    /// <summary>Alteração de binding z/OS Connect (mapeamento API↔transação).</summary>
    ZosConnectBindingChange = 12
}
