using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade Team.
/// Garante que TeamId nunca seja confundido com outro tipo de Guid no sistema.
/// </summary>
public sealed record TeamId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Agregado principal que representa uma equipa dentro do modelo de governança.
/// A equipa é a unidade organizacional responsável por serviços, contratos e domínios.
/// Toda atribuição de ownership, delegação e responsabilidade operacional
/// passa pela equipa como entidade central da governança multi-team.
/// </summary>
public sealed class Team : Entity<TeamId>
{
    /// <summary>
    /// Nome técnico/identificador único da equipa (ex: "platform-core").
    /// Imutável após criação — utilizado como referência estável.
    /// </summary>
    public string Name { get; private init; } = string.Empty;

    /// <summary>
    /// Nome de exibição legível da equipa (ex: "Platform Core Team").
    /// Pode ser atualizado ao longo do ciclo de vida da equipa.
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Descrição opcional da equipa, incluindo missão e responsabilidades.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Estado atual do ciclo de vida da equipa.
    /// Controla visibilidade e operações permitidas.
    /// </summary>
    public TeamStatus Status { get; private set; }

    /// <summary>
    /// Unidade organizacional pai, quando aplicável (ex: "Engineering", "Product").
    /// Permite hierarquização opcional dentro da estrutura da organização.
    /// </summary>
    public string? ParentOrganizationUnit { get; private set; }

    /// <summary>Data/hora UTC de criação da equipa.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última atualização da equipa.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Construtor privado para EF Core e serialização.</summary>
    private Team() { }

    /// <summary>
    /// Cria uma nova equipa com validação de invariantes.
    /// O nome e o display name são obrigatórios; o estado inicial é sempre Active.
    /// </summary>
    /// <param name="name">Nome técnico da equipa (máx. 100 caracteres).</param>
    /// <param name="displayName">Nome de exibição (máx. 200 caracteres).</param>
    /// <param name="description">Descrição opcional (máx. 500 caracteres).</param>
    /// <param name="parentOrganizationUnit">Unidade organizacional pai opcional (máx. 200 caracteres).</param>
    /// <returns>Nova instância válida de Team.</returns>
    public static Team Create(
        string name,
        string displayName,
        string? description = null,
        string? parentOrganizationUnit = null)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.StringTooLong(name, 100, nameof(name));
        Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
        Guard.Against.StringTooLong(displayName, 200, nameof(displayName));

        if (description is not null)
            Guard.Against.StringTooLong(description, 500, nameof(description));

        if (parentOrganizationUnit is not null)
            Guard.Against.StringTooLong(parentOrganizationUnit, 200, nameof(parentOrganizationUnit));

        return new Team
        {
            Id = new TeamId(Guid.NewGuid()),
            Name = name.Trim(),
            DisplayName = displayName.Trim(),
            Description = description?.Trim(),
            Status = TeamStatus.Active,
            ParentOrganizationUnit = parentOrganizationUnit?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Atualiza os dados mutáveis da equipa.
    /// O nome técnico não pode ser alterado após criação.
    /// </summary>
    /// <param name="displayName">Novo nome de exibição (máx. 200 caracteres).</param>
    /// <param name="description">Nova descrição opcional (máx. 500 caracteres).</param>
    /// <param name="parentOrganizationUnit">Nova unidade organizacional pai (máx. 200 caracteres).</param>
    public void Update(
        string displayName,
        string? description,
        string? parentOrganizationUnit)
    {
        Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
        Guard.Against.StringTooLong(displayName, 200, nameof(displayName));

        if (description is not null)
            Guard.Against.StringTooLong(description, 500, nameof(description));

        if (parentOrganizationUnit is not null)
            Guard.Against.StringTooLong(parentOrganizationUnit, 200, nameof(parentOrganizationUnit));

        DisplayName = displayName.Trim();
        Description = description?.Trim();
        ParentOrganizationUnit = parentOrganizationUnit?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Ativa a equipa, permitindo operações e atribuições.
    /// </summary>
    public void Activate()
    {
        Status = TeamStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Desativa a equipa temporariamente.
    /// A equipa permanece no sistema mas não pode receber novas atribuições.
    /// </summary>
    public void Deactivate()
    {
        Status = TeamStatus.Inactive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Arquiva a equipa permanentemente.
    /// Mantida apenas para histórico e auditoria — não pode ser reativada diretamente.
    /// </summary>
    public void Archive()
    {
        Status = TeamStatus.Archived;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
