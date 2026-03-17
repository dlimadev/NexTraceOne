namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Relevância da fonte de conhecimento para um caso de uso específico.
/// Determina o peso da fonte na composição do contexto de enriquecimento.
/// </summary>
public enum AISourceRelevance
{
    /// <summary>Fonte principal — essencial para a qualidade da resposta.</summary>
    Primary,

    /// <summary>Fonte secundária — enriquece a resposta sem ser essencial.</summary>
    Secondary,

    /// <summary>Fonte complementar — usada apenas se disponível e relevante.</summary>
    Supplementary,

    /// <summary>Fonte excluída — não relevante para o caso de uso atual.</summary>
    Excluded
}
