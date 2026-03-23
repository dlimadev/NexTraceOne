namespace NexTraceOne.Notifications.Domain.Enums;

/// <summary>
/// Severidades oficiais de notificação da plataforma NexTraceOne.
/// A severidade determina prioridade visual, elegibilidade de canal,
/// comportamento de acknowledge e regras de roteamento.
/// </summary>
public enum NotificationSeverity
{
    /// <summary>
    /// Informacional — nenhuma ação necessária.
    /// Prioridade visual: neutra/cinza.
    /// Canais: apenas central interna (por padrão).
    /// Acknowledge: não exigido.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Ação recomendada ou requerida dentro de prazo razoável.
    /// Prioridade visual: azul/destaque.
    /// Canais: central interna + email (conforme preferência).
    /// Acknowledge: pode ser exigido dependendo do tipo de evento.
    /// </summary>
    ActionRequired = 1,

    /// <summary>
    /// Atenção necessária — situação pode degradar ou escalar.
    /// Prioridade visual: amarelo/laranja.
    /// Canais: central interna + email + Teams (conforme severidade e preferência).
    /// Acknowledge: recomendado.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Ação imediata necessária — impacto em produção ou segurança.
    /// Prioridade visual: vermelho.
    /// Canais: todos os canais configurados.
    /// Acknowledge: obrigatório.
    /// </summary>
    Critical = 3
}
