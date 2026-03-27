namespace NexTraceOne.AuditCompliance.Application.Retention;

/// <summary>
/// Configuração do job de retenção de auditoria.
/// </summary>
public sealed class AuditRetentionOptions
{
    public const string SectionName = "Audit:Retention";

    /// <summary>Intervalo em minutos entre ciclos de retenção.</summary>
    public int JobIntervalMinutes { get; set; } = 60;

    /// <summary>Delay inicial em segundos antes do primeiro ciclo.</summary>
    public int JobStartupDelaySeconds { get; set; } = 30;
}
