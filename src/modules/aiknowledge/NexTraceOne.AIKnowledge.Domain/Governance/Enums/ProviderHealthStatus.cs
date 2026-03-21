namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Estado de saúde persistido de um provedor de IA, atualizado pelo health check periódico.
/// </summary>
public enum ProviderHealthStatus
{
    /// <summary>Estado desconhecido — health check nunca foi executado.</summary>
    Unknown,

    /// <summary>Provedor saudável — respondendo normalmente.</summary>
    Healthy,

    /// <summary>Provedor degradado — respondendo com latência elevada ou erros parciais.</summary>
    Degraded,

    /// <summary>Provedor indisponível — não responde ou responde com erro.</summary>
    Unhealthy,

    /// <summary>Provedor offline — desligado deliberadamente.</summary>
    Offline
}
