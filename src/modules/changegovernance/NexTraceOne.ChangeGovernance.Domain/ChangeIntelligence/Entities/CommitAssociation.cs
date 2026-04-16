using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

/// <summary>
/// Entidade que representa a associação de um commit ao pool de uma release.
/// Um commit pode existir sem release (Unassigned), em candidatura (Candidate),
/// formalmente incluído (Included) ou excluído pelo PO/PM (Excluded).
///
/// O commit pool é event-driven: commits chegam via webhook do CI/CD
/// e são associados a releases no momento da promoção ou manualmente.
/// </summary>
public sealed class CommitAssociation : Entity<CommitAssociationId>
{
    private CommitAssociation() { }

    /// <summary>SHA do commit git (short ou full).</summary>
    public string CommitSha { get; private set; } = string.Empty;

    /// <summary>Mensagem do commit.</summary>
    public string CommitMessage { get; private set; } = string.Empty;

    /// <summary>Autor do commit (nome ou email).</summary>
    public string CommitAuthor { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC em que o commit foi efectuado.</summary>
    public DateTimeOffset CommittedAt { get; private set; }

    /// <summary>Branch de onde o commit provém (ex: feature/PAY-1234).</summary>
    public string BranchName { get; private set; } = string.Empty;

    /// <summary>Identificador do serviço ao qual o commit pertence (derivado da configuração de repositório).</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Identificador do tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Identificador da release a que este commit está vinculado.
    /// Null quando o commit está em estado Unassigned ou Candidate.
    /// </summary>
    public ReleaseId? ReleaseId { get; private set; }

    /// <summary>Estado de associação do commit ao ciclo de vida de release.</summary>
    public CommitAssignmentStatus AssignmentStatus { get; private set; } = CommitAssignmentStatus.Unassigned;

    /// <summary>Data/hora UTC em que o commit foi associado a uma release.</summary>
    public DateTimeOffset? AssignedAt { get; private set; }

    /// <summary>Utilizador ou sistema que efectuou a associação.</summary>
    public string? AssignedBy { get; private set; }

    /// <summary>Fonte da associação: Manual | AutoPromotion | ExternalWebhook.</summary>
    public string? AssignmentSource { get; private set; }

    /// <summary>
    /// Referências de work items extraídas da mensagem do commit por regex configurável
    /// (ex: PAY-1234, #42, AB#567). Armazenadas como lista separada por vírgula.
    /// </summary>
    public string? ExtractedWorkItemRefs { get; private set; }

    /// <summary>Data/hora UTC em que a entidade foi criada.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Cria um novo CommitAssociation a partir de um evento de push recebido do CI/CD.
    /// O estado inicial é Unassigned, podendo evoluir para Candidate quando uma release activa
    /// corresponder ao branch.
    /// </summary>
    public static CommitAssociation Create(
        Guid tenantId,
        string commitSha,
        string commitMessage,
        string commitAuthor,
        DateTimeOffset committedAt,
        string branchName,
        string serviceName,
        string? extractedWorkItemRefs,
        DateTimeOffset createdAt)
    {
        Guard.Against.NullOrWhiteSpace(commitSha);
        Guard.Against.NullOrWhiteSpace(commitMessage);
        Guard.Against.NullOrWhiteSpace(commitAuthor);
        Guard.Against.NullOrWhiteSpace(branchName);
        Guard.Against.NullOrWhiteSpace(serviceName);

        return new CommitAssociation
        {
            Id = CommitAssociationId.New(),
            TenantId = tenantId,
            CommitSha = commitSha,
            CommitMessage = commitMessage,
            CommitAuthor = commitAuthor,
            CommittedAt = committedAt,
            BranchName = branchName,
            ServiceName = serviceName,
            AssignmentStatus = CommitAssignmentStatus.Unassigned,
            ExtractedWorkItemRefs = extractedWorkItemRefs,
            CreatedAt = createdAt,
        };
    }

    /// <summary>Eleva o commit para estado Candidate (branch corresponde a uma release activa).</summary>
    public void MarkAsCandidate()
    {
        if (AssignmentStatus == CommitAssignmentStatus.Unassigned)
            AssignmentStatus = CommitAssignmentStatus.Candidate;
    }

    /// <summary>Inclui o commit numa release — transição definitiva para Included.</summary>
    public void IncludeInRelease(ReleaseId releaseId, string assignedBy, string source, DateTimeOffset assignedAt)
    {
        Guard.Against.Null(releaseId);
        Guard.Against.NullOrWhiteSpace(assignedBy);
        Guard.Against.NullOrWhiteSpace(source);

        ReleaseId = releaseId;
        AssignmentStatus = CommitAssignmentStatus.Included;
        AssignedBy = assignedBy;
        AssignmentSource = source;
        AssignedAt = assignedAt;
    }

    /// <summary>
    /// Exclui o commit da release — pode ser revertido pelo PO/PM.
    /// O commit fica disponível para associação à próxima release.
    /// </summary>
    public void ExcludeFromRelease(string excludedBy, DateTimeOffset excludedAt)
    {
        Guard.Against.NullOrWhiteSpace(excludedBy);

        ReleaseId = null;
        AssignmentStatus = CommitAssignmentStatus.Excluded;
        AssignedBy = excludedBy;
        AssignedAt = excludedAt;
        AssignmentSource = "Manual";
    }
}

/// <summary>Identificador fortemente tipado de CommitAssociation.</summary>
public sealed record CommitAssociationId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static CommitAssociationId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static CommitAssociationId From(Guid id) => new(id);
}
