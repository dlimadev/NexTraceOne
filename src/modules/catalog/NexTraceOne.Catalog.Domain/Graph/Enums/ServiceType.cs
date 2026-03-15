namespace NexTraceOne.Catalog.Domain.Graph.Enums;

/// <summary>
/// Tipo de serviço no catálogo de serviços.
/// Classifica a natureza técnica do serviço para governança e contexto operacional.
/// </summary>
public enum ServiceType
{
    /// <summary>API REST padrão.</summary>
    RestApi = 0,

    /// <summary>Serviço SOAP (legado ou integração).</summary>
    SoapService = 1,

    /// <summary>Produtor Kafka — publica eventos ou mensagens.</summary>
    KafkaProducer = 2,

    /// <summary>Consumidor Kafka — consome eventos ou mensagens.</summary>
    KafkaConsumer = 3,

    /// <summary>Serviço de background (worker, daemon).</summary>
    BackgroundService = 4,

    /// <summary>Processo agendado (scheduled job/cron).</summary>
    ScheduledProcess = 5,

    /// <summary>Componente de integração (adapter, gateway, bridge).</summary>
    IntegrationComponent = 6,

    /// <summary>Serviço de plataforma partilhado (shared infra, auth, config).</summary>
    SharedPlatformService = 7
}
