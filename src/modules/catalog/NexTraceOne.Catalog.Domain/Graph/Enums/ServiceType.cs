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
    SharedPlatformService = 7,

    // ── Valores já no DB constraint mas anteriormente ausentes no enum ──

    /// <summary>API GraphQL.</summary>
    GraphqlApi = 8,

    /// <summary>Serviço gRPC.</summary>
    GrpcService = 9,

    /// <summary>Sistema legado genérico (não mainframe).</summary>
    LegacySystem = 10,

    /// <summary>API Gateway (Kong, Apigee, etc.).</summary>
    Gateway = 11,

    /// <summary>Serviço de terceiros (externo à organização).</summary>
    ThirdParty = 12,

    // ── Novos valores para core systems / mainframe ──

    /// <summary>Programa COBOL — unidade de execução mainframe.</summary>
    CobolProgram = 13,

    /// <summary>Transação CICS — processamento online mainframe.</summary>
    CicsTransaction = 14,

    /// <summary>Transação IMS — processamento IMS/DB mainframe.</summary>
    ImsTransaction = 15,

    /// <summary>Batch Job — execução batch (JCL, scheduling).</summary>
    BatchJob = 16,

    /// <summary>Sistema mainframe (LPAR, sysplex, região).</summary>
    MainframeSystem = 17,

    /// <summary>Queue Manager IBM MQ — gestor de filas de mensagens.</summary>
    MqQueueManager = 18,

    /// <summary>API z/OS Connect — exposição de transações mainframe via REST.</summary>
    ZosConnectApi = 19,

    /// <summary>Framework / SDK interno — biblioteca ou framework partilhado com desenvolvimento próprio.</summary>
    Framework = 20
}
