namespace NexTraceOne.BuildingBlocks.Core.Notifications;

/// <summary>
/// Categorias de negócio para notificações.
/// Permite ao usuário configurar preferências por categoria
/// e ao sistema rotear para templates e canais apropriados.
/// </summary>
public enum NotificationCategory
{
    /// <summary>Notificações relacionadas a workflows de aprovação e promoção.</summary>
    Workflow = 0,

    /// <summary>Notificações de pedidos de aprovação pendentes ou concluídos.</summary>
    Approval = 1,

    /// <summary>Alertas de segurança: tentativas de login, sessões, break glass.</summary>
    Security = 2,

    /// <summary>Eventos de licenciamento: expiração, renovação, limites de capacidade.</summary>
    Licensing = 3,

    /// <summary>Mudanças em contratos de API: breaking changes, novas versões.</summary>
    Contract = 4,

    /// <summary>Eventos de deployment: promoção entre ambientes, falhas.</summary>
    Deployment = 5,

    /// <summary>Notificações de sistema: manutenção, atualizações, saúde da plataforma.</summary>
    System = 6,

    /// <summary>Comunicações gerais entre usuários ou equipes dentro da plataforma.</summary>
    Communication = 7,

    /// <summary>Revisões periódicas de acesso e permissões (access review campaigns).</summary>
    AccessReview = 8
}
