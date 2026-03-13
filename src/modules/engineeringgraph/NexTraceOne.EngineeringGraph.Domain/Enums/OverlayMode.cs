namespace NexTraceOne.EngineeringGraph.Domain.Enums;

/// <summary>
/// Modos de overlay disponíveis no grafo de engenharia.
/// Cada modo representa uma dimensão de análise visual,
/// com métrica própria, thresholds e legenda explicável.
/// O overlay modifica cor, tamanho e badges dos nós na visualização.
/// </summary>
public enum OverlayMode
{
    /// <summary>Sem overlay — visualização padrão do grafo.</summary>
    None = 0,

    /// <summary>Saúde operacional — baseada em status de deploy, incidentes e SLA.</summary>
    Health = 1,

    /// <summary>Velocidade de mudanças — frequência de releases/deploys recentes.</summary>
    ChangeVelocity = 2,

    /// <summary>Risco — score composto por breaking changes, blast radius e dívida técnica.</summary>
    Risk = 3,

    /// <summary>Custo — estimativa de custo operacional ou de infraestrutura.</summary>
    Cost = 4,

    /// <summary>Dívida de observabilidade — gaps em tracing, métricas, logs e alertas.</summary>
    ObservabilityDebt = 5
}
