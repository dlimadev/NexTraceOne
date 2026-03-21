namespace NexTraceOne.IdentityAccess.Domain.Enums;

/// <summary>
/// Criticidade operacional de um ambiente.
/// Determina o nível de proteção, auditoria e rigor nas operações de mudança.
/// Ambientes de criticidade alta ou crítica exigem aprovações extras e auditorias reforçadas.
/// </summary>
public enum EnvironmentCriticality
{
    /// <summary>Criticidade baixa. Ambientes internos de desenvolvimento sem impacto externo.</summary>
    Low = 1,

    /// <summary>Criticidade média. Ambientes de validação com impacto limitado.</summary>
    Medium = 2,

    /// <summary>Criticidade alta. Ambientes de staging ou UAT com visibilidade externa.</summary>
    High = 3,

    /// <summary>Criticidade crítica. Ambientes de produção ou DR com impacto direto em clientes.</summary>
    Critical = 4
}
