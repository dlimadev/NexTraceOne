using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade TeamDomainLink.
/// Garante que TeamDomainLinkId nunca seja confundido com outro tipo de Guid.
/// </summary>
public sealed record TeamDomainLinkId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Entidade de associação entre uma equipa e um domínio de negócio.
/// Representa a relação de ownership que a equipa exerce sobre o domínio,
/// permitindo governança multi-team onde diferentes equipas podem ter
/// diferentes tipos de responsabilidade (Primary, Shared, Delegated)
/// sobre o mesmo domínio.
/// </summary>
public sealed class TeamDomainLink : Entity<TeamDomainLinkId>
{
    /// <summary>
    /// Identificador da equipa associada ao domínio.
    /// </summary>
    public TeamId TeamId { get; private init; } = default!;

    /// <summary>
    /// Identificador do domínio de negócio associado à equipa.
    /// </summary>
    public GovernanceDomainId DomainId { get; private init; } = default!;

    /// <summary>
    /// Tipo de ownership que a equipa exerce sobre o domínio.
    /// Define o nível de responsabilidade e autoridade.
    /// </summary>
    public OwnershipType OwnershipType { get; private set; }

    /// <summary>Data/hora UTC em que a associação foi criada.</summary>
    public DateTimeOffset LinkedAt { get; private init; }

    /// <summary>Construtor privado para EF Core e serialização.</summary>
    private TeamDomainLink() { }

    /// <summary>
    /// Cria uma nova associação entre equipa e domínio com validação de invariantes.
    /// </summary>
    /// <param name="teamId">Identificador da equipa (obrigatório).</param>
    /// <param name="domainId">Identificador do domínio (obrigatório).</param>
    /// <param name="ownershipType">Tipo de ownership da equipa sobre o domínio.</param>
    /// <returns>Nova instância válida de TeamDomainLink.</returns>
    public static TeamDomainLink Create(
        TeamId teamId,
        GovernanceDomainId domainId,
        OwnershipType ownershipType)
    {
        Guard.Against.Null(teamId, nameof(teamId));
        Guard.Against.Default(teamId.Value, nameof(teamId));
        Guard.Against.Null(domainId, nameof(domainId));
        Guard.Against.Default(domainId.Value, nameof(domainId));
        Guard.Against.EnumOutOfRange(ownershipType, nameof(ownershipType));

        return new TeamDomainLink
        {
            Id = new TeamDomainLinkId(Guid.NewGuid()),
            TeamId = teamId,
            DomainId = domainId,
            OwnershipType = ownershipType,
            LinkedAt = DateTimeOffset.UtcNow
        };
    }
}
