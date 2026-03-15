namespace NexTraceOne.Contracts.Domain.Enums;

/// <summary>
/// Estado do draft de contrato no fluxo de edição do Contract Studio.
/// Controla a progressão desde a criação até a publicação ou descarte.
/// </summary>
public enum DraftStatus
{
    /// <summary>Em edição — draft está sendo trabalhado pelo autor.</summary>
    Editing = 0,

    /// <summary>Em revisão — aguardando aprovação de um revisor.</summary>
    InReview = 1,

    /// <summary>Aprovado — pronto para ser publicado como versão oficial.</summary>
    Approved = 2,

    /// <summary>Publicado — draft já foi promovido a versão oficial.</summary>
    Published = 3,

    /// <summary>Descartado — draft foi abandonado.</summary>
    Discarded = 4
}
