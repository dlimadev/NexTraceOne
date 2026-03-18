namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Estado do fluxo de promoção de conhecimento gerado por IA externa
/// para a memória partilhada da organização.
/// </summary>
public enum AiKnowledgePromotionStatus
{
    /// <summary>Registo criado, aguardando triagem.</summary>
    Pending = 0,

    /// <summary>Em revisão por um responsável humano.</summary>
    UnderReview = 1,

    /// <summary>Aprovado para promoção à memória partilhada.</summary>
    Approved = 2,

    /// <summary>Rejeitado — conteúdo não será promovido.</summary>
    Rejected = 3,

    /// <summary>Já promovido e disponível na memória partilhada.</summary>
    Promoted = 4
}
