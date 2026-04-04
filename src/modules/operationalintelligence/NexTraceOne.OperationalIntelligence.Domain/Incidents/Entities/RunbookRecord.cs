using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

/// <summary>
/// Entidade que representa um runbook operacional —
/// guia passo-a-passo para lidar com tipos específicos de incidentes.
/// </summary>
public sealed class RunbookRecord : AuditableEntity<RunbookRecordId>
{
    private RunbookRecord() { }

    /// <summary>Título do runbook.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Descrição/resumo do runbook.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Serviço associado (opcional).</summary>
    public string? LinkedService { get; private set; }

    /// <summary>Tipo de incidente associado (opcional).</summary>
    public string? LinkedIncidentType { get; private set; }

    /// <summary>Passos do runbook (JSON).</summary>
    public string? StepsJson { get; private set; }

    /// <summary>Pré-requisitos (JSON).</summary>
    public string? PrerequisitesJson { get; private set; }

    /// <summary>Notas de validação pós-execução.</summary>
    public string? PostNotes { get; private set; }

    /// <summary>Responsável pela manutenção do runbook.</summary>
    public string MaintainedBy { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC de publicação.</summary>
    public DateTimeOffset PublishedAt { get; private set; }

    /// <summary>Data/hora UTC da última revisão.</summary>
    public DateTimeOffset? LastReviewedAt { get; private set; }

    /// <summary>Factory method para criação de um RunbookRecord.</summary>
    public static RunbookRecord Create(
        RunbookRecordId id,
        string title,
        string description,
        string? linkedService,
        string? linkedIncidentType,
        string? stepsJson,
        string? prerequisitesJson,
        string? postNotes,
        string maintainedBy,
        DateTimeOffset publishedAt,
        DateTimeOffset? lastReviewedAt = null)
    {
        Guard.Against.NullOrWhiteSpace(title);
        Guard.Against.NullOrWhiteSpace(description);
        Guard.Against.NullOrWhiteSpace(maintainedBy);

        return new RunbookRecord
        {
            Id = id,
            Title = title,
            Description = description,
            LinkedService = linkedService,
            LinkedIncidentType = linkedIncidentType,
            StepsJson = stepsJson,
            PrerequisitesJson = prerequisitesJson,
            PostNotes = postNotes,
            MaintainedBy = maintainedBy,
            PublishedAt = publishedAt,
            LastReviewedAt = lastReviewedAt,
        };
    }

    /// <summary>Atualiza os campos editáveis do runbook.</summary>
    public void Update(
        string title,
        string description,
        string? linkedService,
        string? linkedIncidentType,
        string? stepsJson,
        string? prerequisitesJson,
        string? postNotes,
        string maintainedBy,
        DateTimeOffset reviewedAt)
    {
        Guard.Against.NullOrWhiteSpace(title);
        Guard.Against.NullOrWhiteSpace(description);
        Guard.Against.NullOrWhiteSpace(maintainedBy);

        Title = title;
        Description = description;
        LinkedService = linkedService;
        LinkedIncidentType = linkedIncidentType;
        StepsJson = stepsJson;
        PrerequisitesJson = prerequisitesJson;
        PostNotes = postNotes;
        MaintainedBy = maintainedBy;
        LastReviewedAt = reviewedAt;
    }
}

/// <summary>Identificador fortemente tipado de RunbookRecord.</summary>
public sealed record RunbookRecordId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static RunbookRecordId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static RunbookRecordId From(Guid id) => new(id);
}
