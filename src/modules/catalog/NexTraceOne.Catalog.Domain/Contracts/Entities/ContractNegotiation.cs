using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade que representa uma negociação cross-team de contrato — semelhante a um
/// "Pull Request para contratos". Permite que equipas proponham, revisem, negociem
/// e aprovem ou rejeitem alterações contratuais de forma colaborativa e rastreável.
/// </summary>
public sealed class ContractNegotiation : AuditableEntity<ContractNegotiationId>
{
    private ContractNegotiation() { }

    /// <summary>Identificador do contrato existente (nullable para negociações de novos contratos).</summary>
    public Guid? ContractId { get; private set; }

    /// <summary>Identificador da equipa que propôs a negociação.</summary>
    public Guid ProposedByTeamId { get; private set; }

    /// <summary>Nome da equipa proponente para exibição.</summary>
    public string ProposedByTeamName { get; private set; } = string.Empty;

    /// <summary>Título descritivo da negociação.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Descrição detalhada da proposta de negociação.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Estado atual da negociação.</summary>
    public NegotiationStatus Status { get; private set; }

    /// <summary>Prazo opcional para conclusão da negociação.</summary>
    public DateTimeOffset? Deadline { get; private set; }

    /// <summary>Lista de TeamIds participantes (serializado JSONB).</summary>
    public string Participants { get; private set; } = string.Empty;

    /// <summary>Número de equipas participantes.</summary>
    public int ParticipantCount { get; private set; }

    /// <summary>Número total de comentários na negociação.</summary>
    public int CommentCount { get; private set; }

    /// <summary>Especificação/diff do contrato proposto (serializado JSONB).</summary>
    public string? ProposedContractSpec { get; private set; }

    /// <summary>Momento de criação da negociação.</summary>
    public new DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Momento da última atividade na negociação.</summary>
    public DateTimeOffset LastActivityAt { get; private set; }

    /// <summary>Momento de resolução (aprovação ou rejeição).</summary>
    public DateTimeOffset? ResolvedAt { get; private set; }

    /// <summary>Identificador do utilizador que resolveu a negociação.</summary>
    public string? ResolvedByUserId { get; private set; }

    /// <summary>Identificador do utilizador que iniciou a negociação.</summary>
    public string InitiatedByUserId { get; private set; } = string.Empty;

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Cria uma nova negociação de contrato com estado Draft.
    /// </summary>
    public static ContractNegotiation Create(
        Guid? contractId,
        Guid proposedByTeamId,
        string proposedByTeamName,
        string title,
        string description,
        DateTimeOffset? deadline,
        string participants,
        int participantCount,
        string? proposedContractSpec,
        string initiatedByUserId,
        DateTimeOffset createdAt)
    {
        Guard.Against.Default(proposedByTeamId);
        Guard.Against.NullOrWhiteSpace(proposedByTeamName);
        Guard.Against.NullOrWhiteSpace(title);
        Guard.Against.NullOrWhiteSpace(description);
        Guard.Against.NullOrWhiteSpace(participants);
        Guard.Against.NegativeOrZero(participantCount);
        Guard.Against.NullOrWhiteSpace(initiatedByUserId);

        return new ContractNegotiation
        {
            Id = ContractNegotiationId.New(),
            ContractId = contractId,
            ProposedByTeamId = proposedByTeamId,
            ProposedByTeamName = proposedByTeamName,
            Title = title,
            Description = description,
            Status = NegotiationStatus.Draft,
            Deadline = deadline,
            Participants = participants,
            ParticipantCount = participantCount,
            CommentCount = 0,
            ProposedContractSpec = proposedContractSpec,
            CreatedAt = createdAt,
            LastActivityAt = createdAt,
            InitiatedByUserId = initiatedByUserId
        };
    }

    /// <summary>
    /// Submete a negociação para revisão pelas equipas participantes.
    /// Transição: Draft → InReview.
    /// </summary>
    public void SubmitForReview(DateTimeOffset submittedAt)
    {
        EnsureValidTransition(NegotiationStatus.Draft, NegotiationStatus.InReview);
        Status = NegotiationStatus.InReview;
        LastActivityAt = submittedAt;
    }

    /// <summary>
    /// Inicia a fase de negociação ativa entre as equipas.
    /// Transição: InReview → Negotiating.
    /// </summary>
    public void StartNegotiating(DateTimeOffset startedAt)
    {
        EnsureValidTransition(NegotiationStatus.InReview, NegotiationStatus.Negotiating);
        Status = NegotiationStatus.Negotiating;
        LastActivityAt = startedAt;
    }

    /// <summary>
    /// Aprova a negociação — contrato aceite pelas partes.
    /// Transição: InReview/Negotiating → Approved.
    /// </summary>
    public void Approve(string resolvedByUserId, DateTimeOffset resolvedAt)
    {
        Guard.Against.NullOrWhiteSpace(resolvedByUserId);

        if (Status is not (NegotiationStatus.InReview or NegotiationStatus.Negotiating))
            throw new InvalidOperationException(
                $"Cannot transition from '{Status}' to '{NegotiationStatus.Approved}'.");

        Status = NegotiationStatus.Approved;
        ResolvedByUserId = resolvedByUserId;
        ResolvedAt = resolvedAt;
        LastActivityAt = resolvedAt;
    }

    /// <summary>
    /// Rejeita a negociação — proposta recusada.
    /// Transição: InReview/Negotiating → Rejected.
    /// </summary>
    public void Reject(string resolvedByUserId, DateTimeOffset resolvedAt)
    {
        Guard.Against.NullOrWhiteSpace(resolvedByUserId);

        if (Status is not (NegotiationStatus.InReview or NegotiationStatus.Negotiating))
            throw new InvalidOperationException(
                $"Cannot transition from '{Status}' to '{NegotiationStatus.Rejected}'.");

        Status = NegotiationStatus.Rejected;
        ResolvedByUserId = resolvedByUserId;
        ResolvedAt = resolvedAt;
        LastActivityAt = resolvedAt;
    }

    /// <summary>
    /// Regista um novo comentário na negociação, incrementando o contador e atualizando a última atividade.
    /// </summary>
    public void AddComment(DateTimeOffset commentedAt)
    {
        CommentCount++;
        LastActivityAt = commentedAt;
    }

    private void EnsureValidTransition(NegotiationStatus expectedFrom, NegotiationStatus to)
    {
        if (Status != expectedFrom)
            throw new InvalidOperationException(
                $"Cannot transition from '{Status}' to '{to}'.");
    }
}

/// <summary>Identificador fortemente tipado de ContractNegotiation.</summary>
public sealed record ContractNegotiationId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ContractNegotiationId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ContractNegotiationId From(Guid id) => new(id);
}
