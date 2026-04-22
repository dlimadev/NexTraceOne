namespace NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

/// <summary>
/// Nível de maturidade de postura Zero Trust de um serviço.
/// Calculado a partir do ZeroTrustScore agregado por 4 dimensões ponderadas:
/// autenticação (30 pts), mTLS (25 pts), rotação de tokens (20 pts) e cobertura de políticas (25 pts).
/// Wave AD.1 — Zero Trust Posture Report.
/// </summary>
public enum ZeroTrustTier
{
    /// <summary>Score &gt;= 85 — controlo Zero Trust plenamente aplicado.</summary>
    Enforced,

    /// <summary>Score &gt;= 65 — postura controlada, com lacunas menores.</summary>
    Controlled,

    /// <summary>Score &gt;= 40 — cobertura parcial, exposição moderada.</summary>
    Partial,

    /// <summary>Score &lt; 40 — serviço exposto, intervenção necessária.</summary>
    Exposed
}
