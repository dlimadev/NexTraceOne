using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade GovernanceDomain.
/// Garante que GovernanceDomainId nunca seja confundido com outro tipo de Guid.
/// </summary>
public sealed record GovernanceDomainId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Representa um domínio de negócio dentro do modelo de governança da plataforma.
/// O domínio agrupa capacidades de negócio, serviços e contratos sob uma
/// classificação de criticidade. É a unidade de contexto que permite
/// governança multi-domínio com visibilidade sobre impacto e blast radius.
/// Nota: Classe nomeada GovernanceDomain para evitar conflito com System.
/// </summary>
public sealed class GovernanceDomain : Entity<GovernanceDomainId>
{
    /// <summary>
    /// Nome técnico/identificador único do domínio (ex: "payments", "identity").
    /// Imutável após criação — utilizado como referência estável.
    /// </summary>
    public string Name { get; private init; } = string.Empty;

    /// <summary>
    /// Nome de exibição legível do domínio (ex: "Payments and Billing").
    /// Pode ser atualizado ao longo do ciclo de vida.
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Descrição opcional do domínio, incluindo âmbito e responsabilidades.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Nível de criticidade do domínio para priorização operacional.
    /// Afeta decisões de blast radius, incidentes e SLAs.
    /// </summary>
    public DomainCriticality Criticality { get; private set; }

    /// <summary>
    /// Classificação de capacidade de negócio associada ao domínio (ex: "Core", "Supporting", "Generic").
    /// Permite categorização estratégica alinhada a Domain-Driven Design.
    /// </summary>
    public string? CapabilityClassification { get; private set; }

    /// <summary>Data/hora UTC de criação do domínio.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última atualização do domínio.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>
    /// Token de concorrência otimista (PostgreSQL xmin).
    /// Utilizado pelo EF Core para detetar conflitos de escrita concorrente.
    /// </summary>
    public uint RowVersion { get; set; }

    /// <summary>Construtor privado para EF Core e serialização.</summary>
    private GovernanceDomain() { }

    /// <summary>
    /// Cria um novo domínio de negócio com validação de invariantes.
    /// O nome e o display name são obrigatórios; a criticidade padrão é Medium.
    /// </summary>
    /// <param name="name">Nome técnico do domínio (máx. 100 caracteres).</param>
    /// <param name="displayName">Nome de exibição (máx. 200 caracteres).</param>
    /// <param name="description">Descrição opcional (máx. 500 caracteres).</param>
    /// <param name="criticality">Nível de criticidade do domínio.</param>
    /// <param name="capabilityClassification">Classificação de capacidade (máx. 200 caracteres).</param>
    /// <returns>Nova instância válida de GovernanceDomain.</returns>
    public static GovernanceDomain Create(
        string name,
        string displayName,
        string? description = null,
        DomainCriticality criticality = DomainCriticality.Medium,
        string? capabilityClassification = null)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.StringTooLong(name, 100, nameof(name));
        Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
        Guard.Against.StringTooLong(displayName, 200, nameof(displayName));

        if (description is not null)
            Guard.Against.StringTooLong(description, 500, nameof(description));

        if (capabilityClassification is not null)
            Guard.Against.StringTooLong(capabilityClassification, 200, nameof(capabilityClassification));

        Guard.Against.EnumOutOfRange(criticality, nameof(criticality));

        return new GovernanceDomain
        {
            Id = new GovernanceDomainId(Guid.NewGuid()),
            Name = name.Trim(),
            DisplayName = displayName.Trim(),
            Description = description?.Trim(),
            Criticality = criticality,
            CapabilityClassification = capabilityClassification?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Atualiza os dados mutáveis do domínio de governança.
    /// O nome técnico não pode ser alterado após criação.
    /// </summary>
    /// <param name="displayName">Novo nome de exibição (máx. 200 caracteres).</param>
    /// <param name="description">Nova descrição opcional (máx. 500 caracteres).</param>
    /// <param name="criticality">Novo nível de criticidade.</param>
    /// <param name="capabilityClassification">Nova classificação de capacidade (máx. 200 caracteres).</param>
    public void Update(
        string displayName,
        string? description,
        DomainCriticality criticality,
        string? capabilityClassification)
    {
        Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
        Guard.Against.StringTooLong(displayName, 200, nameof(displayName));

        if (description is not null)
            Guard.Against.StringTooLong(description, 500, nameof(description));

        Guard.Against.EnumOutOfRange(criticality, nameof(criticality));

        if (capabilityClassification is not null)
            Guard.Against.StringTooLong(capabilityClassification, 200, nameof(capabilityClassification));

        DisplayName = displayName.Trim();
        Description = description?.Trim();
        Criticality = criticality;
        CapabilityClassification = capabilityClassification?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
