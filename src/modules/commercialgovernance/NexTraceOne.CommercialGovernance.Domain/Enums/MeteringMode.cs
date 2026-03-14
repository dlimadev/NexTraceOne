namespace NexTraceOne.Licensing.Domain.Enums;

/// <summary>
/// Modo de medição de uso que define como o consumo é rastreado e reportado.
/// Determina se a telemetria de uso é coletada em tempo real, por snapshot
/// periódico ou desabilitada (air-gapped).
/// </summary>
public enum MeteringMode
{
    /// <summary>Medição em tempo real via eventos (padrão SaaS).</summary>
    RealTime = 0,

    /// <summary>Snapshots periódicos de uso (self-hosted com conectividade).</summary>
    Periodic = 1,

    /// <summary>Medição manual/offline para ambientes isolados.</summary>
    Manual = 2,

    /// <summary>Sem medição de uso (air-gapped ou enterprise especial).</summary>
    Disabled = 3
}
