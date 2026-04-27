namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Ciclo de vida de um CustomDashboard (V3.6 — Governance, Reports &amp; Embedding).
/// Permite gestão de deprecação e arquivo de dashboards.
/// </summary>
public enum DashboardLifecycleStatus
{
    /// <summary>Rascunho — visível apenas ao criador, não publicado.</summary>
    Draft = 0,

    /// <summary>Publicado — visível conforme SharingPolicy.</summary>
    Published = 1,

    /// <summary>Deprecado — ainda acessível mas com aviso; aponta para substituto.</summary>
    Deprecated = 2,

    /// <summary>Arquivado — inacessível para utilizadores normais; preservado para auditoria.</summary>
    Archived = 3
}
