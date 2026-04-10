namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

/// <summary>
/// Tipos de evento que alteram a confiança de uma mudança ao longo do tempo.
/// </summary>
public enum ConfidenceEventType
{
    /// <summary>Mudança criada — confiança inicial baseada em risco.</summary>
    Created = 0,

    /// <summary>Validação concluída em ambiente de desenvolvimento.</summary>
    DevValidated = 1,

    /// <summary>Testes concluídos em staging/pre-production.</summary>
    StagingTested = 2,

    /// <summary>Aprovação recebida para promoção.</summary>
    Approved = 3,

    /// <summary>Anomalia detectada que reduz a confiança.</summary>
    AnomalyDetected = 4,

    /// <summary>Deploy executado em ambiente de produção.</summary>
    Deployed = 5,

    /// <summary>Validação pós-deploy bem-sucedida.</summary>
    PostDeployValidated = 6,

    /// <summary>Incidente correlacionado com esta mudança.</summary>
    IncidentCorrelated = 7,

    /// <summary>Rollback executado.</summary>
    RolledBack = 8,

    /// <summary>Mitigação aplicada após problema.</summary>
    Mitigated = 9,

    /// <summary>Gate de promoção avaliado.</summary>
    GateEvaluated = 10,

    /// <summary>Evento manual registrado por operador.</summary>
    ManualOverride = 11
}
