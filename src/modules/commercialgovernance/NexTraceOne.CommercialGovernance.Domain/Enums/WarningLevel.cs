namespace NexTraceOne.Licensing.Domain.Enums;

/// <summary>
/// Nível de alerta progressivo para consumo de quotas.
/// Usado para gerar avisos e CTAs de expansão em dashboards e notificações.
///
/// Fluxo:
/// - Normal (abaixo de 70%) → sem ação.
/// - Advisory (70%) → sugestão de limpeza/arquivamento no dashboard.
/// - Warning (85%) → notificação para admins com CTA de expansão.
/// - Critical (95%) → alerta urgente com CTA de expansão emergencial.
/// - Exceeded (100%) → bloqueio de novos recursos afetados.
/// </summary>
public enum WarningLevel
{
    /// <summary>Consumo normal — sem alertas.</summary>
    Normal = 0,

    /// <summary>70% do limite — sugestão de otimização.</summary>
    Advisory = 1,

    /// <summary>85% do limite — notificação para administradores.</summary>
    Warning = 2,

    /// <summary>95% do limite — alerta crítico com opção de expansão emergencial.</summary>
    Critical = 3,

    /// <summary>100% do limite — bloqueio de novos recursos afetados.</summary>
    Exceeded = 4
}
