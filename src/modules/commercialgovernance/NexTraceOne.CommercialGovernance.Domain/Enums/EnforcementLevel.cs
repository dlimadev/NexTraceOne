namespace NexTraceOne.Licensing.Domain.Enums;

/// <summary>
/// Nível de enforcement aplicado quando um limite é atingido.
/// Define se o sistema deve apenas alertar (soft) ou bloquear (hard).
///
/// Decisão de design:
/// - SoftLimit permite exceder o limite com avisos, ideal para não interromper operações críticas.
/// - HardLimit bloqueia imediatamente novas operações ao atingir o limite.
/// - NeverBreak garante que recursos já ativos nunca sejam desabilitados, apenas novos são bloqueados.
/// </summary>
public enum EnforcementLevel
{
    /// <summary>Aviso sem bloqueio — permite exceder o limite com registro de overage.</summary>
    Soft = 0,

    /// <summary>Bloqueio rígido — impede novas operações ao atingir o limite.</summary>
    Hard = 1,

    /// <summary>Nunca interrompe recursos já ativos — bloqueia apenas criação de novos.</summary>
    NeverBreak = 2
}
