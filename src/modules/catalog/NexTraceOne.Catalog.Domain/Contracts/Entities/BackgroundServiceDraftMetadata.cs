using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade que armazena metadados específicos de Background Service para um ContractDraft em edição.
/// Permite que o Contract Studio mantenha informações do processo (nome, categoria, trigger, schedule,
/// inputs/outputs, side effects, messaging role e tópicos consumidos/produzidos) desacopladas do SpecContent genérico do draft.
/// Vinculada a um ContractDraft com ContractType = BackgroundService.
/// </summary>
public sealed class BackgroundServiceDraftMetadata : Entity<BackgroundServiceDraftMetadataId>
{
    private BackgroundServiceDraftMetadata() { }

    /// <summary>Identificador do draft de contrato de background service ao qual este metadado pertence.</summary>
    public ContractDraftId ContractDraftId { get; private set; } = null!;

    /// <summary>Nome do processo em background definido no draft.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Categoria do processo: Job, Worker, Scheduler, Processor, Exporter, Notifier, etc.</summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>Tipo de trigger: Cron, Interval, EventTriggered, OnDemand, Continuous.</summary>
    public string TriggerType { get; private set; } = "OnDemand";

    /// <summary>Expressão de schedule/trigger do processo (opcional).</summary>
    public string? ScheduleExpression { get; private set; }

    /// <summary>JSON de inputs esperados do processo.</summary>
    public string InputsJson { get; private set; } = "{}";

    /// <summary>JSON de outputs produzidos pelo processo.</summary>
    public string OutputsJson { get; private set; } = "{}";

    /// <summary>JSON de side effects declarados do processo.</summary>
    public string SideEffectsJson { get; private set; } = "[]";

    // ── Messaging Role ──────────────────────────────────────────────────────────

    /// <summary>
    /// Role de messaging do background service: None, Producer, Consumer, Both.
    /// Indica se este processo produz e/ou consome mensagens de tópicos/filas.
    /// </summary>
    public string MessagingRole { get; private set; } = "None";

    /// <summary>JSON dos tópicos/filas consumidos pelo processo.</summary>
    public string ConsumedTopicsJson { get; private set; } = "[]";

    /// <summary>JSON dos tópicos/filas produzidos pelo processo.</summary>
    public string ProducedTopicsJson { get; private set; } = "[]";

    /// <summary>JSON dos serviços consumidos pelo processo.</summary>
    public string ConsumedServicesJson { get; private set; } = "[]";

    /// <summary>JSON dos eventos produzidos pelo processo.</summary>
    public string ProducedEventsJson { get; private set; } = "[]";

    /// <summary>
    /// Cria novos metadados de background service para um draft de contrato.
    /// </summary>
    public static BackgroundServiceDraftMetadata Create(
        ContractDraftId contractDraftId,
        string serviceName,
        string category = "Job",
        string triggerType = "OnDemand",
        string? scheduleExpression = null,
        string inputsJson = "{}",
        string outputsJson = "{}",
        string sideEffectsJson = "[]",
        string messagingRole = "None",
        string consumedTopicsJson = "[]",
        string producedTopicsJson = "[]",
        string consumedServicesJson = "[]",
        string producedEventsJson = "[]")
    {
        Guard.Against.Null(contractDraftId);
        Guard.Against.NullOrWhiteSpace(serviceName);

        return new BackgroundServiceDraftMetadata
        {
            Id = BackgroundServiceDraftMetadataId.New(),
            ContractDraftId = contractDraftId,
            ServiceName = serviceName,
            Category = category,
            TriggerType = triggerType,
            ScheduleExpression = scheduleExpression,
            InputsJson = inputsJson,
            OutputsJson = outputsJson,
            SideEffectsJson = sideEffectsJson,
            MessagingRole = messagingRole,
            ConsumedTopicsJson = consumedTopicsJson,
            ProducedTopicsJson = producedTopicsJson,
            ConsumedServicesJson = consumedServicesJson,
            ProducedEventsJson = producedEventsJson
        };
    }

    /// <summary>Atualiza os metadados do draft de background service quando o utilizador edita no Studio.</summary>
    public void Update(
        string serviceName,
        string category,
        string triggerType,
        string? scheduleExpression,
        string inputsJson,
        string outputsJson,
        string sideEffectsJson,
        string messagingRole = "None",
        string consumedTopicsJson = "[]",
        string producedTopicsJson = "[]",
        string consumedServicesJson = "[]",
        string producedEventsJson = "[]")
    {
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(category);

        ServiceName = serviceName;
        Category = category;
        TriggerType = triggerType;
        ScheduleExpression = scheduleExpression;
        InputsJson = inputsJson;
        OutputsJson = outputsJson;
        SideEffectsJson = sideEffectsJson;
        MessagingRole = messagingRole;
        ConsumedTopicsJson = consumedTopicsJson;
        ProducedTopicsJson = producedTopicsJson;
        ConsumedServicesJson = consumedServicesJson;
        ProducedEventsJson = producedEventsJson;
    }
}

/// <summary>Identificador fortemente tipado de BackgroundServiceDraftMetadata.</summary>
public sealed record BackgroundServiceDraftMetadataId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static BackgroundServiceDraftMetadataId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static BackgroundServiceDraftMetadataId From(Guid id) => new(id);
}
