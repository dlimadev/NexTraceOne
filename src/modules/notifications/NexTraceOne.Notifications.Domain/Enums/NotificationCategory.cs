namespace NexTraceOne.Notifications.Domain.Enums;

/// <summary>
/// Categorias oficiais de notificação da plataforma NexTraceOne.
/// Cada notificação pertence exatamente a uma categoria, que permite
/// filtragem, roteamento e preferências por área funcional.
/// </summary>
public enum NotificationCategory
{
    /// <summary>Incidentes operacionais, degradações, anomalias.</summary>
    Incident = 0,

    /// <summary>Aprovações pendentes, rejeitadas, expiradas.</summary>
    Approval = 1,

    /// <summary>Releases, promoções, deployments, mudanças.</summary>
    Change = 2,

    /// <summary>Contratos de API, SOAP, eventos, breaking changes.</summary>
    Contract = 3,

    /// <summary>Segurança, acesso, break-glass, JIT, segredos.</summary>
    Security = 4,

    /// <summary>Compliance, evidências, políticas, checks.</summary>
    Compliance = 5,

    /// <summary>Custos, budgets, anomalias financeiras, desperdício.</summary>
    FinOps = 6,

    /// <summary>IA, providers, tokens, policies de IA, drafts.</summary>
    AI = 7,

    /// <summary>Integrações, conectores, ingestão, webhooks.</summary>
    Integration = 8,

    /// <summary>Plataforma, health, backups, pipelines, workers.</summary>
    Platform = 9,

    /// <summary>Informacional / genérico sem ação requerida.</summary>
    Informational = 10
}
