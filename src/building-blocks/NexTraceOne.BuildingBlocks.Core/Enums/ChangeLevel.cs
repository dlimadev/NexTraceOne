namespace NexTraceOne.BuildingBlocks.Core.Enums;

/// <summary>
/// Taxonomia de níveis de mudança da plataforma NexTraceOne.
/// Define a gravidade e o fluxo de governança necessário para cada tipo de mudança.
/// </summary>
public enum ChangeLevel
{
    /// <summary>Nível 0 — Eventos operacionais. Sem versão, sem workflow.</summary>
    Operational = 0,

    /// <summary>Nível 1 — Mudança sem alteração de contrato. Patch version.</summary>
    NonBreaking = 1,

    /// <summary>Nível 2 — Mudança com contrato non-breaking. Minor version.</summary>
    Additive = 2,

    /// <summary>Nível 3 — Mudança com contrato breaking. MAJOR version.</summary>
    Breaking = 3,

    /// <summary>Nível 4 — Eventos de publicação. Sem nova versão.</summary>
    Publication = 4
}
