namespace NexTraceOne.BuildingBlocks.Observability.Alerting.Models;

/// <summary>
/// Nível de severidade de um alerta operacional.
/// Utilizado para classificar a urgência e rotear para os canais adequados.
/// </summary>
public enum AlertSeverity
{
    /// <summary>Informacional — sem ação necessária.</summary>
    Info = 0,

    /// <summary>Aviso — requer atenção, mas sem impacto imediato.</summary>
    Warning = 1,

    /// <summary>Erro — impacto em funcionalidade ou serviço.</summary>
    Error = 2,

    /// <summary>Crítico — impacto severo em produção, ação imediata necessária.</summary>
    Critical = 3
}
