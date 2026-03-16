using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade DelegatedAdministration.
/// Garante que DelegatedAdministrationId nunca seja confundido com outro tipo de Guid.
/// </summary>
public sealed record DelegatedAdministrationId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Representa uma delegação de permissões administrativas a um utilizador.
/// Permite que administradores deleguem âmbitos específicos de gestão
/// (equipa, domínio, leitura ou administração completa) a outros utilizadores,
/// com rastreabilidade completa de quem delegou, quando e porquê.
/// Suporta expiração automática e revogação explícita.
/// </summary>
public sealed class DelegatedAdministration : Entity<DelegatedAdministrationId>
{
    /// <summary>
    /// Identificador do utilizador que recebe a delegação.
    /// </summary>
    public string GranteeUserId { get; private init; } = string.Empty;

    /// <summary>
    /// Nome de exibição do utilizador delegado, para consulta rápida sem join.
    /// </summary>
    public string GranteeDisplayName { get; private init; } = string.Empty;

    /// <summary>
    /// Âmbito de permissões concedidas pela delegação.
    /// </summary>
    public DelegationScope Scope { get; private init; }

    /// <summary>
    /// Identificador da equipa, quando a delegação é limitada ao contexto de uma equipa.
    /// Null se a delegação não for específica de equipa.
    /// </summary>
    public string? TeamId { get; private init; }

    /// <summary>
    /// Identificador do domínio, quando a delegação é limitada ao contexto de um domínio.
    /// Null se a delegação não for específica de domínio.
    /// </summary>
    public string? DomainId { get; private init; }

    /// <summary>
    /// Justificação obrigatória para a delegação, para fins de auditoria e compliance.
    /// </summary>
    public string Reason { get; private init; } = string.Empty;

    /// <summary>
    /// Indica se a delegação está ativa. False após revogação explícita.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>Data/hora UTC em que a delegação foi concedida.</summary>
    public DateTimeOffset GrantedAt { get; private init; }

    /// <summary>Data/hora UTC de expiração da delegação (null se sem expiração).</summary>
    public DateTimeOffset? ExpiresAt { get; private init; }

    /// <summary>Data/hora UTC em que a delegação foi revogada (null se ainda ativa).</summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Construtor privado para EF Core e serialização.</summary>
    private DelegatedAdministration() { }

    /// <summary>
    /// Cria uma nova delegação de administração com validação de invariantes.
    /// A delegação nasce ativa e pode ser revogada ou expirar automaticamente.
    /// </summary>
    /// <param name="granteeUserId">Id do utilizador que recebe a delegação (obrigatório).</param>
    /// <param name="granteeDisplayName">Nome de exibição do delegado (obrigatório).</param>
    /// <param name="scope">Âmbito de permissões concedidas.</param>
    /// <param name="teamId">Id da equipa, se delegação com âmbito de equipa.</param>
    /// <param name="domainId">Id do domínio, se delegação com âmbito de domínio.</param>
    /// <param name="reason">Justificação obrigatória para auditoria.</param>
    /// <param name="expiresAt">Data/hora UTC de expiração opcional.</param>
    /// <returns>Nova instância válida de DelegatedAdministration.</returns>
    public static DelegatedAdministration Create(
        string granteeUserId,
        string granteeDisplayName,
        DelegationScope scope,
        string? teamId,
        string? domainId,
        string reason,
        DateTimeOffset? expiresAt = null)
    {
        Guard.Against.NullOrWhiteSpace(granteeUserId, nameof(granteeUserId));
        Guard.Against.NullOrWhiteSpace(granteeDisplayName, nameof(granteeDisplayName));
        Guard.Against.EnumOutOfRange(scope, nameof(scope));
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));

        return new DelegatedAdministration
        {
            Id = new DelegatedAdministrationId(Guid.NewGuid()),
            GranteeUserId = granteeUserId.Trim(),
            GranteeDisplayName = granteeDisplayName.Trim(),
            Scope = scope,
            TeamId = teamId?.Trim(),
            DomainId = domainId?.Trim(),
            Reason = reason.Trim(),
            IsActive = true,
            GrantedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
            RevokedAt = null
        };
    }

    /// <summary>
    /// Revoga a delegação, marcando-a como inativa com data/hora de revogação.
    /// Operação idempotente — invocar sobre delegação já revogada não tem efeito.
    /// </summary>
    public void Revoke()
    {
        if (!IsActive) return;

        IsActive = false;
        RevokedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Verifica se a delegação expirou com base na data/hora UTC atual.
    /// Delegações sem data de expiração nunca expiram automaticamente.
    /// </summary>
    /// <returns>True se a delegação expirou; false caso contrário.</returns>
    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt.Value <= DateTimeOffset.UtcNow;
    }
}
