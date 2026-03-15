namespace NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

/// <summary>
/// Flags operacionais que sinalizam condições relevantes para a confiabilidade de um serviço.
/// Complementam o ReliabilityStatus fornecendo contexto sobre a causa ou risco associado.
/// Extensível para futuras fases (mitigação, IA operacional).
/// </summary>
[Flags]
public enum OperationalFlag
{
    /// <summary>Nenhuma flag ativa — serviço sem sinais operacionais relevantes.</summary>
    None = 0,

    /// <summary>Mudança recente com impacto potencial na confiabilidade do serviço.</summary>
    RecentChangeImpact = 1,

    /// <summary>Incidente ativo ou recente associado ao serviço.</summary>
    IncidentLinked = 2,

    /// <summary>Anomalia comportamental detectada em métricas operacionais.</summary>
    AnomalyDetected = 4,

    /// <summary>Risco identificado em dependências do serviço.</summary>
    DependencyRisk = 8,

    /// <summary>Cobertura operacional insuficiente — faltam runbooks, monitoramento ou ownership.</summary>
    CoverageGap = 16
}
