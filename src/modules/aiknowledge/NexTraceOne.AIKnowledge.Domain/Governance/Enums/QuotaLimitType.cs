namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Tipo de limite aplicado a uma política de quota de tokens.
/// Determina o comportamento quando o limite é atingido.
/// </summary>
public enum QuotaLimitType
{
    /// <summary>Limite flexível — gera aviso mas permite continuar.</summary>
    Soft = 0,

    /// <summary>Limite rígido — bloqueia pedidos que excedam a quota.</summary>
    Hard = 1
}
