using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Knowledge.Domain.Entities;

public enum ProposedRunbookStatus { Proposed, UnderReview, Approved, Rejected }

/// <summary>
/// Runbook proposto automaticamente pelo sistema a partir de um incidente resolvido.
/// Representa a sugestão de criar um runbook operacional como artefato de conhecimento.
/// Owner: Knowledge module. Pilar: Source of Truth &amp; Operational Knowledge.
/// </summary>
public sealed class ProposedRunbook : Entity<ProposedRunbookId>
{
    private ProposedRunbook() { }

    public string Title { get; private set; } = string.Empty;
    public string ContentMarkdown { get; private set; } = string.Empty;
    public Guid SourceIncidentId { get; private set; }
    public string? ServiceName { get; private set; }
    public string? TeamName { get; private set; }
    public ProposedRunbookStatus Status { get; private set; }
    public DateTimeOffset ProposedAt { get; private set; }
    public string? ReviewedBy { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public string? ReviewNote { get; private set; }

    public static ProposedRunbook Create(
        string title,
        string contentMarkdown,
        Guid sourceIncidentId,
        DateTimeOffset proposedAt,
        string? serviceName = null,
        string? teamName = null)
    {
        Guard.Against.NullOrWhiteSpace(title);
        Guard.Against.NullOrWhiteSpace(contentMarkdown);
        Guard.Against.Default(sourceIncidentId);

        return new ProposedRunbook
        {
            Id = ProposedRunbookId.New(),
            Title = title,
            ContentMarkdown = contentMarkdown,
            SourceIncidentId = sourceIncidentId,
            ServiceName = serviceName,
            TeamName = teamName,
            Status = ProposedRunbookStatus.Proposed,
            ProposedAt = proposedAt
        };
    }

    public void Approve(string reviewedBy, DateTimeOffset at, string? note = null)
    {
        Guard.Against.NullOrWhiteSpace(reviewedBy);
        Status = ProposedRunbookStatus.Approved;
        ReviewedBy = reviewedBy;
        ReviewedAt = at;
        ReviewNote = note;
    }

    public void Reject(string reviewedBy, DateTimeOffset at, string note)
    {
        Guard.Against.NullOrWhiteSpace(reviewedBy);
        Guard.Against.NullOrWhiteSpace(note);
        Status = ProposedRunbookStatus.Rejected;
        ReviewedBy = reviewedBy;
        ReviewedAt = at;
        ReviewNote = note;
    }
}

/// <summary>Identificador fortemente tipado de ProposedRunbook.</summary>
public sealed record ProposedRunbookId(Guid Value) : TypedIdBase(Value)
{
    public static ProposedRunbookId New() => new(Guid.NewGuid());
    public static ProposedRunbookId From(Guid id) => new(id);
}
