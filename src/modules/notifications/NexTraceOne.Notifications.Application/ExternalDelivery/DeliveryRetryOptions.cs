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
}
