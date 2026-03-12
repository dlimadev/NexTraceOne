namespace NexTraceOne.Promotion.Domain.Enums;

/// <summary>
/// Status do ciclo de vida de uma solicitação de promoção entre ambientes.
/// </summary>
public enum PromotionStatus
{
    /// <summary>Pendente — aguardando início da avaliação.</summary>
    Pending = 0,

    /// <summary>Em avaliação — gates estão sendo verificados.</summary>
    InEvaluation = 1,

    /// <summary>Aprovada — todos os gates foram satisfeitos e a promoção foi aceita.</summary>
    Approved = 2,

    /// <summary>Rejeitada — um ou mais gates obrigatórios falharam.</summary>
    Rejected = 3,

    /// <summary>Bloqueada — promoção impedida por regra de governança.</summary>
    Blocked = 4,

    /// <summary>Cancelada — solicitação cancelada antes da conclusão.</summary>
    Cancelled = 5
}
