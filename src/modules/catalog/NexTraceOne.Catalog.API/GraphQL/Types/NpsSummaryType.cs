namespace NexTraceOne.Catalog.API.GraphQL.Types;

/// <summary>
/// Tipo GraphQL que representa o sumário agregado de NPS de uma equipa.
/// Orientado a liderança técnica e product — não a utilizadores finais individuais.
/// Persona: Tech Lead, Product, Executive.
/// </summary>
public sealed class NpsSummaryType
{
    /// <summary>Identificador da equipa.</summary>
    public string TeamId { get; init; } = string.Empty;

    /// <summary>Período de análise (ex: 2026-Q1).</summary>
    public string? Period { get; init; }

    /// <summary>Total de respostas neste período.</summary>
    public int TotalResponses { get; init; }

    /// <summary>NPS calculado (-100 a +100).</summary>
    public decimal NpsScore { get; init; }

    /// <summary>Percentagem de Promotores (score 9-10).</summary>
    public decimal PromoterPercent { get; init; }

    /// <summary>Percentagem de Passivos (score 7-8).</summary>
    public decimal PassivePercent { get; init; }

    /// <summary>Percentagem de Detratores (score 0-6).</summary>
    public decimal DetractorPercent { get; init; }

    /// <summary>Número absoluto de Promotores.</summary>
    public int PromoterCount { get; init; }

    /// <summary>Número absoluto de Passivos.</summary>
    public int PassiveCount { get; init; }

    /// <summary>Número absoluto de Detratores.</summary>
    public int DetractorCount { get; init; }

    /// <summary>Satisfação média com ferramentas (0-10).</summary>
    public decimal AvgToolSatisfaction { get; init; }

    /// <summary>Satisfação média com processos (0-10).</summary>
    public decimal AvgProcessSatisfaction { get; init; }

    /// <summary>Satisfação média com a plataforma (0-10).</summary>
    public decimal AvgPlatformSatisfaction { get; init; }
}
