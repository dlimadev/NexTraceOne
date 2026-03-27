namespace NexTraceOne.Notifications.Application.ExternalDelivery;

/// <summary>
/// Configuração de retry para entrega externa de notificações.
/// </summary>
public sealed class DeliveryRetryOptions
{
    public const string SectionName = "Notifications:Retry";

    /// <summary>Número máximo de tentativas de entrega por canal (incluindo a primeira).</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Intervalo base entre retries (segundos). Multiplicado pelo número da tentativa.</summary>
    public int BaseDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Intervalo de execução do NotificationDeliveryRetryJob em segundos.
    /// Padrão: 60 segundos (1 minuto).
    /// </summary>
    public int RetryJobIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Número máximo de deliveries a processar por ciclo do NotificationDeliveryRetryJob.
    /// Padrão: 50. Ajustar conforme volume de notificações.
    /// </summary>
    public int RetryJobBatchSize { get; set; } = 50;

    /// <summary>
    /// Tempo de espera inicial (segundos) antes do primeiro ciclo do NotificationDeliveryRetryJob.
    /// Permite que a aplicação inicialize completamente antes de processar retries.
    /// Padrão: 10 segundos.
    /// </summary>
    public int RetryJobStartupDelaySeconds { get; set; } = 10;
}
