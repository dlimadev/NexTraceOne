namespace NexTraceOne.ChangeGovernance.Domain.Promotion.Enums;

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

/// <summary>
/// Modo de aplicação de um gate de qualidade na promoção.
/// Segue o padrão forward-only Advisory → SoftEnforce → HardEnforce.
/// </summary>
public enum CodeQualityGateEnforcement
{
    /// <summary>Apenas informa — nunca bloqueia nem sinaliza como aviso.</summary>
    Advisory = 0,

    /// <summary>Sinaliza como aviso quando o gate falha, mas não bloqueia a promoção.</summary>
    SoftEnforce = 1,

    /// <summary>Bloqueia a promoção quando o gate falha.</summary>
    HardEnforce = 2
}
