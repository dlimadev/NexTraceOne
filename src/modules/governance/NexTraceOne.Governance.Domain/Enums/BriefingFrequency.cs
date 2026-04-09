namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Frequência de geração de executive briefings.
/// Determina a cadência com que o sistema produz resumos executivos automatizados.
/// </summary>
public enum BriefingFrequency
{
    /// <summary>Briefing gerado diariamente.</summary>
    Daily = 1,

    /// <summary>Briefing gerado semanalmente.</summary>
    Weekly = 2,

    /// <summary>Briefing gerado mensalmente.</summary>
    Monthly = 3,

    /// <summary>Briefing gerado a pedido (on-demand).</summary>
    OnDemand = 4
}
