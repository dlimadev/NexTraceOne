namespace NexTraceOne.Catalog.Domain.Graph.Enums;

/// <summary>
/// Tier operacional de um serviço — define os requisitos mínimos de confiabilidade,
/// SLO, gates de promoção e thresholds de maturidade aplicáveis.
/// </summary>
public enum ServiceTierType
{
    /// <summary>Serviço crítico para o negócio — SLO alto, gates rigorosos, monitoring obrigatório.</summary>
    Critical = 1,

    /// <summary>Serviço padrão — boas práticas recomendadas, SLO moderado.</summary>
    Standard = 2,

    /// <summary>Serviço experimental — thresholds relaxados, ownership mínimo obrigatório.</summary>
    Experimental = 3
}
