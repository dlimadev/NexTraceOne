namespace NexTraceOne.Catalog.Domain.Graph.Enums;

/// <summary>
/// Tipo de interface exposta por um serviço no catálogo.
/// Distingue o protocolo e o padrão de comunicação da interface.
/// </summary>
public enum InterfaceType
{
    /// <summary>API REST (HTTP/HTTPS, OpenAPI).</summary>
    RestApi = 0,

    /// <summary>Serviço SOAP (WSDL, XML).</summary>
    SoapService = 1,

    /// <summary>Producer Kafka — publica eventos num tópico.</summary>
    KafkaProducer = 2,

    /// <summary>Consumer Kafka — consome eventos de um tópico.</summary>
    KafkaConsumer = 3,

    /// <summary>Serviço gRPC (Protocol Buffers).</summary>
    GrpcService = 4,

    /// <summary>API GraphQL.</summary>
    GraphqlApi = 5,

    /// <summary>Background worker — processo contínuo sem interface de rede.</summary>
    BackgroundWorker = 6,

    /// <summary>Job agendado (Cron ou schedule).</summary>
    ScheduledJob = 7,

    /// <summary>Webhook producer — notifica subscribers externos via HTTP push.</summary>
    WebhookProducer = 8,

    /// <summary>Webhook consumer — recebe notificações HTTP push de sistemas externos.</summary>
    WebhookConsumer = 9,

    /// <summary>API z/OS Connect — interface REST sobre serviços mainframe.</summary>
    ZosConnectApi = 10,

    /// <summary>Fila MQ — ponto de entrada ou saída via IBM MQ ou similar.</summary>
    MqQueue = 11,

    /// <summary>Bridge de integração — conector entre sistemas heterogéneos.</summary>
    IntegrationBridge = 12
}
