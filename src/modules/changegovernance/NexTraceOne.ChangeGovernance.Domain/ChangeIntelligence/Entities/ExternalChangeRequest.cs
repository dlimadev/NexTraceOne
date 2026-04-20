using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

/// <summary>
/// Entidade que representa um pedido de mudança importado de um sistema externo (ServiceNow, Jira, AzureDevOps, Generic).
/// Permite correlacionar change requests externos com o ciclo de vida de mudanças governado pelo NexTraceOne.
/// </summary>
public sealed class ExternalChangeRequest : Entity<ExternalChangeRequestId>
{
    private ExternalChangeRequest() { }

    /// <summary>Sistema externo de origem (ex: "ServiceNow", "Jira", "AzureDevOps", "Generic").</summary>
    public string ExternalSystem { get; private set; } = string.Empty;

    /// <summary>Identificador do ticket/CR no sistema externo.</summary>
    public string ExternalId { get; private set; } = string.Empty;

    /// <summary>Título do pedido de mudança.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Descrição opcional do pedido de mudança.</summary>
    public string? Description { get; private set; }

    /// <summary>Tipo de mudança ("Normal", "Emergency", "Standard").</summary>
    public string ChangeType { get; private set; } = string.Empty;

    /// <summary>Responsável pelo pedido de mudança no sistema externo.</summary>
    public string RequestedBy { get; private set; } = string.Empty;

    /// <summary>Data/hora de início planeada para a mudança (UTC).</summary>
    public DateTimeOffset? ScheduledStart { get; private set; }

    /// <summary>Data/hora de fim planeada para a mudança (UTC).</summary>
    public DateTimeOffset? ScheduledEnd { get; private set; }

    /// <summary>Identificador do serviço afetado no NexTraceOne (quando conhecido).</summary>
    public Guid? ServiceId { get; private set; }

    /// <summary>Ambiente alvo da mudança (ex: "production", "staging").</summary>
    public string? Environment { get; private set; }

    /// <summary>Estado atual do pedido de mudança externo.</summary>
    public ExternalChangeRequestStatus Status { get; private set; } = ExternalChangeRequestStatus.Pending;

    /// <summary>Data/hora UTC em que o pedido foi ingerido no NexTraceOne.</summary>
    public DateTimeOffset IngestedAt { get; private set; }

    /// <summary>Identificador da Release interna vinculada a este pedido (quando disponível).</summary>
    public Guid? LinkedReleaseId { get; private set; }

    /// <summary>Motivo da rejeição, preenchido quando Status = Rejected.</summary>
    public string? RejectionReason { get; private set; }

    /// <summary>
    /// Cria um novo pedido de mudança externo.
    /// Define o status inicial como <see cref="ExternalChangeRequestStatus.Ingested"/>.
    /// </summary>
    public static ExternalChangeRequest Create(
        string externalSystem,
        string externalId,
        string title,
        string? description,
        string changeType,
        string requestedBy,
        DateTimeOffset? scheduledStart,
        DateTimeOffset? scheduledEnd,
        Guid? serviceId,
        string? environment,
        DateTimeOffset ingestedAt)
    {
        Guard.Against.NullOrWhiteSpace(externalSystem);
        Guard.Against.NullOrWhiteSpace(externalId);
        Guard.Against.NullOrWhiteSpace(title);
        Guard.Against.NullOrWhiteSpace(changeType);
        Guard.Against.NullOrWhiteSpace(requestedBy);

        return new ExternalChangeRequest
        {
            Id = ExternalChangeRequestId.New(),
            ExternalSystem = externalSystem,
            ExternalId = externalId,
            Title = title,
            Description = description,
            ChangeType = changeType,
            RequestedBy = requestedBy,
            ScheduledStart = scheduledStart,
            ScheduledEnd = scheduledEnd,
            ServiceId = serviceId,
            Environment = environment,
            Status = ExternalChangeRequestStatus.Ingested,
            IngestedAt = ingestedAt
        };
    }

    /// <summary>Vincula este pedido de mudança a uma Release interna existente.</summary>
    public void LinkToRelease(Guid releaseId)
    {
        Guard.Against.Default(releaseId);
        LinkedReleaseId = releaseId;
        Status = ExternalChangeRequestStatus.Linked;
    }

    /// <summary>Rejeita este pedido de mudança com o motivo especificado.</summary>
    public void Reject(string reason)
    {
        Guard.Against.NullOrWhiteSpace(reason);
        RejectionReason = reason;
        Status = ExternalChangeRequestStatus.Rejected;
    }
}

/// <summary>Identificador fortemente tipado de ExternalChangeRequest.</summary>
public sealed record ExternalChangeRequestId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ExternalChangeRequestId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ExternalChangeRequestId From(Guid id) => new(id);
}
