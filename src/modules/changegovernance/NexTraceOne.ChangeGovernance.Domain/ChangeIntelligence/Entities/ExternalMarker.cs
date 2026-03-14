using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.ChangeIntelligence.Domain.Enums;

namespace NexTraceOne.ChangeIntelligence.Domain.Entities;

/// <summary>
/// Marcador externo recebido de ferramentas CI/CD (GitHub, GitLab, Jenkins, Azure DevOps).
/// O módulo Change Intelligence é event-driven e observador — não executor.
/// Cada marcador enriquece a timeline da release com eventos do ciclo de vida
/// real do deploy, permitindo correlação entre inteligência e execução.
/// </summary>
public sealed class ExternalMarker : AuditableEntity<ExternalMarkerId>
{
    /// <summary>Release associada a este marcador.</summary>
    public ReleaseId ReleaseId { get; private set; } = null!;

    /// <summary>Tipo do marcador (build, deploy, rollback, canary, migration, etc.).</summary>
    public MarkerType MarkerType { get; private set; }

    /// <summary>Sistema de origem do marcador (ex: GitHub, Jenkins, GitLab).</summary>
    public string SourceSystem { get; private set; } = string.Empty;

    /// <summary>Identificador externo do evento no sistema de origem (ex: run ID, pipeline ID).</summary>
    public string ExternalId { get; private set; } = string.Empty;

    /// <summary>Dados adicionais em formato JSON enviados pela ferramenta externa.</summary>
    public string? Payload { get; private set; }

    /// <summary>Momento em que o evento ocorreu no sistema de origem.</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Momento de recebimento pelo NexTraceOne.</summary>
    public DateTimeOffset ReceivedAt { get; private set; }

    private ExternalMarker() { }

    /// <summary>
    /// Cria um novo marcador externo a partir de um evento de ferramenta CI/CD.
    /// Validação de entrada via guard clauses.
    /// </summary>
    public static ExternalMarker Create(
        ReleaseId releaseId,
        MarkerType markerType,
        string sourceSystem,
        string externalId,
        string? payload,
        DateTimeOffset occurredAt,
        DateTimeOffset receivedAt)
    {
        Guard.Against.Null(releaseId, nameof(releaseId));
        Guard.Against.NullOrWhiteSpace(sourceSystem, nameof(sourceSystem));
        Guard.Against.NullOrWhiteSpace(externalId, nameof(externalId));

        return new ExternalMarker
        {
            Id = ExternalMarkerId.New(),
            ReleaseId = releaseId,
            MarkerType = markerType,
            SourceSystem = sourceSystem,
            ExternalId = externalId,
            Payload = payload,
            OccurredAt = occurredAt,
            ReceivedAt = receivedAt
        };
    }
}

/// <summary>Identificador fortemente tipado para ExternalMarker.</summary>
public sealed record ExternalMarkerId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Gera um novo identificador.</summary>
    public static ExternalMarkerId New() => new(Guid.NewGuid());
    /// <summary>Cria a partir de um Guid existente.</summary>
    public static ExternalMarkerId From(Guid id) => new(id);
}
