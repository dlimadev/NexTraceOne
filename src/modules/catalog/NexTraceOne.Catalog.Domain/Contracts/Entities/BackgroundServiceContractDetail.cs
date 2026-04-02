using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade específica para metadados de Background Service Contracts publicados.
/// Captura informações estruturais que distinguem jobs/workers/schedulers de APIs HTTP e Event Contracts:
/// categoria do processo, schedule/trigger, inputs/outputs esperados, side effects e dependências operacionais.
/// Inclui modelo de messaging role (Producer/Consumer/Both/None) com tópicos consumidos e produzidos.
/// Vinculada a uma ContractVersion com ContractType = BackgroundService.
/// </summary>
public sealed class BackgroundServiceContractDetail : AuditableEntity<BackgroundServiceContractDetailId>
{
    private BackgroundServiceContractDetail() { }

    /// <summary>Identificador da versão de contrato Background Service à qual este detalhe pertence.</summary>
    public ContractVersionId ContractVersionId { get; private set; } = null!;

    /// <summary>Nome do processo em background (ex: "OrderExpirationJob", "ReportGeneratorWorker").</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>
    /// Categoria do processo: Job, Worker, Scheduler, Processor, Exporter, Notifier, etc.
    /// Valor livre que classifica a natureza operacional do processo.
    /// </summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>
    /// Expressão de schedule/trigger do processo.
    /// Exemplos: "0 * * * *" (cron), "PT5M" (ISO 8601 interval), "OnDemand", "EventTriggered".
    /// Nulo quando o processo não tem schedule fixo.
    /// </summary>
    public string? ScheduleExpression { get; private set; }

    /// <summary>
    /// Tipo de trigger do processo: Cron, Interval, EventTriggered, OnDemand, Continuous.
    /// Categoriza como o processo é iniciado.
    /// </summary>
    public string TriggerType { get; private set; } = "OnDemand";

    /// <summary>
    /// JSON descrevendo os inputs esperados pelo processo quando aplicável.
    /// Formato: { "parameterName": "type|description", ... }
    /// </summary>
    public string InputsJson { get; private set; } = "{}";

    /// <summary>
    /// JSON descrevendo os outputs produzidos pelo processo.
    /// Formato: { "outputName": "type|description", ... }
    /// </summary>
    public string OutputsJson { get; private set; } = "{}";

    /// <summary>
    /// JSON descrevendo os side effects operacionais declarados (escrita em BD, eventos publicados, chamadas externas).
    /// Formato: [ "description", ... ]
    /// </summary>
    public string SideEffectsJson { get; private set; } = "[]";

    /// <summary>Timeout máximo declarado para execução do processo (ex: "PT30M", "1h").</summary>
    public string? TimeoutExpression { get; private set; }

    /// <summary>Indica se o processo suporta execução em paralelo / concorrente.</summary>
    public bool AllowsConcurrency { get; private set; }

    // ── Messaging Role ──────────────────────────────────────────────────────────

    /// <summary>
    /// Role de messaging do background service: None, Producer, Consumer, Both.
    /// Indica se este processo produz e/ou consome mensagens de tópicos/filas.
    /// </summary>
    public string MessagingRole { get; private set; } = "None";

    /// <summary>
    /// JSON dos tópicos/filas consumidos pelo processo.
    /// Formato: [{ "topicName": "...", "entityType": "...", "format": "avro|json|protobuf" }]
    /// </summary>
    public string ConsumedTopicsJson { get; private set; } = "[]";

    /// <summary>
    /// JSON dos tópicos/filas produzidos pelo processo.
    /// Formato: [{ "topicName": "...", "entityType": "...", "format": "avro|json|protobuf" }]
    /// </summary>
    public string ProducedTopicsJson { get; private set; } = "[]";

    /// <summary>
    /// JSON dos serviços consumidos pelo processo (REST, gRPC, SOAP).
    /// Formato: [{ "serviceName": "...", "protocol": "REST|gRPC|SOAP" }]
    /// </summary>
    public string ConsumedServicesJson { get; private set; } = "[]";

    /// <summary>
    /// JSON dos eventos produzidos pelo processo para tópicos/destinos.
    /// Formato: [{ "eventName": "...", "targetTopic": "..." }]
    /// </summary>
    public string ProducedEventsJson { get; private set; } = "[]";

    /// <summary>
    /// Cria um novo BackgroundServiceContractDetail com os metadados do processo.
    /// </summary>
    public static Result<BackgroundServiceContractDetail> Create(
        ContractVersionId contractVersionId,
        string serviceName,
        string category,
        string triggerType,
        string inputsJson,
        string outputsJson,
        string sideEffectsJson,
        string? scheduleExpression = null,
        string? timeoutExpression = null,
        bool allowsConcurrency = false,
        string messagingRole = "None",
        string consumedTopicsJson = "[]",
        string producedTopicsJson = "[]",
        string consumedServicesJson = "[]",
        string producedEventsJson = "[]")
    {
        Guard.Against.Null(contractVersionId);
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(category);
        Guard.Against.NullOrWhiteSpace(triggerType);

        return new BackgroundServiceContractDetail
        {
            Id = BackgroundServiceContractDetailId.New(),
            ContractVersionId = contractVersionId,
            ServiceName = serviceName,
            Category = category,
            TriggerType = triggerType,
            InputsJson = inputsJson,
            OutputsJson = outputsJson,
            SideEffectsJson = sideEffectsJson,
            ScheduleExpression = scheduleExpression,
            TimeoutExpression = timeoutExpression,
            AllowsConcurrency = allowsConcurrency,
            MessagingRole = messagingRole,
            ConsumedTopicsJson = consumedTopicsJson,
            ProducedTopicsJson = producedTopicsJson,
            ConsumedServicesJson = consumedServicesJson,
            ProducedEventsJson = producedEventsJson
        };
    }

    /// <summary>Atualiza os metadados do background service após re-registro.</summary>
    public void Update(
        string serviceName,
        string category,
        string triggerType,
        string inputsJson,
        string outputsJson,
        string sideEffectsJson,
        string? scheduleExpression,
        string? timeoutExpression,
        bool allowsConcurrency,
        string messagingRole = "None",
        string consumedTopicsJson = "[]",
        string producedTopicsJson = "[]",
        string consumedServicesJson = "[]",
        string producedEventsJson = "[]")
    {
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(category);
        Guard.Against.NullOrWhiteSpace(triggerType);

        ServiceName = serviceName;
        Category = category;
        TriggerType = triggerType;
        InputsJson = inputsJson;
        OutputsJson = outputsJson;
        SideEffectsJson = sideEffectsJson;
        ScheduleExpression = scheduleExpression;
        TimeoutExpression = timeoutExpression;
        AllowsConcurrency = allowsConcurrency;
        MessagingRole = messagingRole;
        ConsumedTopicsJson = consumedTopicsJson;
        ProducedTopicsJson = producedTopicsJson;
        ConsumedServicesJson = consumedServicesJson;
        ProducedEventsJson = producedEventsJson;
    }
}

/// <summary>Identificador fortemente tipado de BackgroundServiceContractDetail.</summary>
public sealed record BackgroundServiceContractDetailId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static BackgroundServiceContractDetailId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static BackgroundServiceContractDetailId From(Guid id) => new(id);
}
