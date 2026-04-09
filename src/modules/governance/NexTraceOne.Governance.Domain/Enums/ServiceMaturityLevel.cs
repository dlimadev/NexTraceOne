namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Nível de maturidade de um serviço individual no modelo de maturidade de 5 níveis.
/// </summary>
public enum ServiceMaturityLevel
{
    /// <summary>Nível 1 — Básico: serviço registado, ownership definido.</summary>
    Basic = 1,

    /// <summary>Nível 2 — Documentado: contratos publicados, documentação existente.</summary>
    Documented = 2,

    /// <summary>Nível 3 — Governado: políticas aplicadas, approval workflows activos.</summary>
    Governed = 3,

    /// <summary>Nível 4 — Observado: telemetria activa, baselines definidos, alertas configurados.</summary>
    Observed = 4,

    /// <summary>Nível 5 — Resiliente: runbooks, rollback testado, chaos engineering validado.</summary>
    Resilient = 5
}
