using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade GovernanceWaiver.
/// Garante que GovernanceWaiverId nunca seja confundido com outro tipo de Guid no sistema.
/// </summary>
public sealed record GovernanceWaiverId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Representa uma exceção (waiver) concedida a um escopo específico para
/// dispensar a conformidade com uma regra ou pacote de governança completo.
/// Suporta fluxo de aprovação, rejeição e revogação, com justificação
/// obrigatória e links de evidência para auditoria completa.
/// </summary>
public sealed class GovernanceWaiver : Entity<GovernanceWaiverId>
{
    /// <summary>
    /// Identificador do governance pack ao qual o waiver se aplica.
    /// </summary>
    public GovernancePackId PackId { get; private init; } = default!;

    /// <summary>
    /// Identificador da regra específica dispensada.
    /// Null se o waiver se aplica ao pack inteiro.
    /// </summary>
    public string? RuleId { get; private init; }

    /// <summary>
    /// Escopo ao qual o waiver se aplica (ex: nome da equipa, domínio, serviço).
    /// </summary>
    public string Scope { get; private init; } = string.Empty;

    /// <summary>
    /// Tipo de escopo do waiver, determinando o nível de aplicação.
    /// </summary>
    public GovernanceScopeType ScopeType { get; private init; }

    /// <summary>
    /// Justificação obrigatória para o pedido de waiver, para auditoria e compliance.
    /// </summary>
    public string Justification { get; private init; } = string.Empty;

    /// <summary>
    /// Estado atual do waiver no fluxo de aprovação.
    /// </summary>
    public WaiverStatus Status { get; private set; }

    /// <summary>
    /// Identificador do utilizador que solicitou o waiver.
    /// </summary>
    public string RequestedBy { get; private init; } = string.Empty;

    /// <summary>Data/hora UTC do pedido do waiver.</summary>
    public DateTimeOffset RequestedAt { get; private init; }

    /// <summary>
    /// Identificador do utilizador que reviu o waiver (aprovação, rejeição ou revogação).
    /// Null enquanto o waiver estiver pendente.
    /// </summary>
    public string? ReviewedBy { get; private set; }

    /// <summary>
    /// Data/hora UTC da revisão do waiver.
    /// Null enquanto o waiver estiver pendente.
    /// </summary>
    public DateTimeOffset? ReviewedAt { get; private set; }

    /// <summary>
    /// Data/hora UTC de expiração do waiver.
    /// Null se o waiver não tem expiração automática.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; private init; }

    /// <summary>
    /// Links para evidências que suportam o pedido de waiver.
    /// </summary>
    public IReadOnlyList<string> EvidenceLinks { get; private init; } = [];

    /// <summary>
    /// Token de concorrência otimista (PostgreSQL xmin).
    /// Utilizado pelo EF Core para detetar conflitos de escrita concorrente.
    /// </summary>
    public uint RowVersion { get; set; }

    /// <summary>Construtor privado para EF Core e serialização.</summary>
    private GovernanceWaiver() { }

    /// <summary>
    /// Cria um novo waiver de governança com validação de invariantes.
    /// O estado inicial é sempre Pending, aguardando revisão.
    /// </summary>
    /// <param name="packId">Identificador do governance pack.</param>
    /// <param name="ruleId">Identificador da regra específica, ou null para o pack inteiro (máx. 100 caracteres).</param>
    /// <param name="scope">Escopo de aplicação do waiver (máx. 200 caracteres).</param>
    /// <param name="scopeType">Tipo de escopo do waiver.</param>
    /// <param name="justification">Justificação obrigatória (máx. 2000 caracteres).</param>
    /// <param name="requestedBy">Identificador do solicitante (máx. 200 caracteres).</param>
    /// <param name="expiresAt">Data de expiração opcional.</param>
    /// <param name="evidenceLinks">Links de evidência que suportam o pedido.</param>
    /// <returns>Nova instância válida de GovernanceWaiver.</returns>
    public static GovernanceWaiver Create(
        GovernancePackId packId,
        string? ruleId,
        string scope,
        GovernanceScopeType scopeType,
        string justification,
        string requestedBy,
        DateTimeOffset? expiresAt,
        IReadOnlyList<string> evidenceLinks)
    {
        Guard.Against.Null(packId, nameof(packId));

        if (ruleId is not null)
            Guard.Against.StringTooLong(ruleId, 100, nameof(ruleId));

        Guard.Against.NullOrWhiteSpace(scope, nameof(scope));
        Guard.Against.StringTooLong(scope, 200, nameof(scope));
        Guard.Against.EnumOutOfRange(scopeType, nameof(scopeType));
        Guard.Against.NullOrWhiteSpace(justification, nameof(justification));
        Guard.Against.StringTooLong(justification, 2000, nameof(justification));
        Guard.Against.NullOrWhiteSpace(requestedBy, nameof(requestedBy));
        Guard.Against.StringTooLong(requestedBy, 200, nameof(requestedBy));
        Guard.Against.Null(evidenceLinks, nameof(evidenceLinks));

        return new GovernanceWaiver
        {
            Id = new GovernanceWaiverId(Guid.NewGuid()),
            PackId = packId,
            RuleId = ruleId?.Trim(),
            Scope = scope.Trim(),
            ScopeType = scopeType,
            Justification = justification.Trim(),
            Status = WaiverStatus.Pending,
            RequestedBy = requestedBy.Trim(),
            RequestedAt = DateTimeOffset.UtcNow,
            ReviewedBy = null,
            ReviewedAt = null,
            ExpiresAt = expiresAt,
            EvidenceLinks = evidenceLinks
        };
    }

    /// <summary>
    /// Aprova o waiver, registando o revisor e a data de aprovação.
    /// Apenas waivers em Pending podem ser aprovados.
    /// </summary>
    /// <param name="reviewedBy">Identificador do utilizador que aprovou (máx. 200 caracteres).</param>
    /// <exception cref="InvalidOperationException">Se o waiver não estiver Pending.</exception>
    public void Approve(string reviewedBy)
    {
        Guard.Against.NullOrWhiteSpace(reviewedBy, nameof(reviewedBy));
        Guard.Against.StringTooLong(reviewedBy, 200, nameof(reviewedBy));

        if (Status != WaiverStatus.Pending)
            throw new InvalidOperationException($"Cannot approve a waiver with status '{Status}'. Only Pending waivers can be approved.");

        Status = WaiverStatus.Approved;
        ReviewedBy = reviewedBy.Trim();
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Rejeita o waiver, registando o revisor e a data de rejeição.
    /// Apenas waivers em Pending podem ser rejeitados.
    /// </summary>
    /// <param name="reviewedBy">Identificador do utilizador que rejeitou (máx. 200 caracteres).</param>
    /// <exception cref="InvalidOperationException">Se o waiver não estiver Pending.</exception>
    public void Reject(string reviewedBy)
    {
        Guard.Against.NullOrWhiteSpace(reviewedBy, nameof(reviewedBy));
        Guard.Against.StringTooLong(reviewedBy, 200, nameof(reviewedBy));

        if (Status != WaiverStatus.Pending)
            throw new InvalidOperationException($"Cannot reject a waiver with status '{Status}'. Only Pending waivers can be rejected.");

        Status = WaiverStatus.Rejected;
        ReviewedBy = reviewedBy.Trim();
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Revoga um waiver previamente aprovado, registando o revisor e a data de revogação.
    /// Apenas waivers Approved podem ser revogados.
    /// </summary>
    /// <param name="reviewedBy">Identificador do utilizador que revogou (máx. 200 caracteres).</param>
    /// <exception cref="InvalidOperationException">Se o waiver não estiver Approved.</exception>
    public void Revoke(string reviewedBy)
    {
        Guard.Against.NullOrWhiteSpace(reviewedBy, nameof(reviewedBy));
        Guard.Against.StringTooLong(reviewedBy, 200, nameof(reviewedBy));

        if (Status != WaiverStatus.Approved)
            throw new InvalidOperationException($"Cannot revoke a waiver with status '{Status}'. Only Approved waivers can be revoked.");

        Status = WaiverStatus.Revoked;
        ReviewedBy = reviewedBy.Trim();
        ReviewedAt = DateTimeOffset.UtcNow;
    }
}
