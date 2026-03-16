using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade GovernancePack.
/// Garante que GovernancePackId nunca seja confundido com outro tipo de Guid no sistema.
/// </summary>
public sealed record GovernancePackId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Representa um pacote de governança que agrupa regras e políticas aplicáveis
/// a serviços, contratos, mudanças e operações dentro da plataforma.
/// O pack é a unidade de distribuição e aplicação de governança, com
/// versionamento, publicação e ciclo de vida controlado.
/// Aggregate root responsável pelo ciclo de vida do governance pack.
/// </summary>
public sealed class GovernancePack : Entity<GovernancePackId>
{
    /// <summary>
    /// Nome técnico/identificador único do pack (ex: "api-contract-standards").
    /// Imutável após criação — utilizado como referência estável.
    /// </summary>
    public string Name { get; private init; } = string.Empty;

    /// <summary>
    /// Nome de exibição legível do pack (ex: "API Contract Standards").
    /// Pode ser atualizado ao longo do ciclo de vida.
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Descrição opcional do pack, incluindo propósito e âmbito de aplicação.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Categoria principal do pack, determinando o domínio de governança abrangido.
    /// </summary>
    public GovernanceRuleCategory Category { get; private set; }

    /// <summary>
    /// Estado atual do ciclo de vida do pack.
    /// Controla visibilidade, distribuição e operações permitidas.
    /// </summary>
    public GovernancePackStatus Status { get; private set; }

    /// <summary>
    /// Versão atualmente publicada do pack (ex: "1.0.0").
    /// Null enquanto o pack estiver em rascunho e nunca tiver sido publicado.
    /// </summary>
    public string? CurrentVersion { get; private set; }

    /// <summary>Data/hora UTC de criação do pack.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última atualização do pack.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Construtor privado para EF Core e serialização.</summary>
    private GovernancePack() { }

    /// <summary>
    /// Cria um novo governance pack com validação de invariantes.
    /// O estado inicial é sempre Draft. A versão fica null até à primeira publicação.
    /// </summary>
    /// <param name="name">Nome técnico do pack (máx. 100 caracteres).</param>
    /// <param name="displayName">Nome de exibição (máx. 200 caracteres).</param>
    /// <param name="description">Descrição opcional (máx. 1000 caracteres).</param>
    /// <param name="category">Categoria do pack.</param>
    /// <returns>Nova instância válida de GovernancePack.</returns>
    public static GovernancePack Create(
        string name,
        string displayName,
        string? description,
        GovernanceRuleCategory category)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.StringTooLong(name, 100, nameof(name));
        Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
        Guard.Against.StringTooLong(displayName, 200, nameof(displayName));

        if (description is not null)
            Guard.Against.StringTooLong(description, 1000, nameof(description));

        Guard.Against.EnumOutOfRange(category, nameof(category));

        return new GovernancePack
        {
            Id = new GovernancePackId(Guid.NewGuid()),
            Name = name.Trim(),
            DisplayName = displayName.Trim(),
            Description = description?.Trim(),
            Category = category,
            Status = GovernancePackStatus.Draft,
            CurrentVersion = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Atualiza os dados mutáveis do governance pack.
    /// O nome técnico não pode ser alterado após criação.
    /// </summary>
    /// <param name="displayName">Novo nome de exibição (máx. 200 caracteres).</param>
    /// <param name="description">Nova descrição opcional (máx. 1000 caracteres).</param>
    /// <param name="category">Nova categoria do pack.</param>
    public void Update(
        string displayName,
        string? description,
        GovernanceRuleCategory category)
    {
        Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
        Guard.Against.StringTooLong(displayName, 200, nameof(displayName));

        if (description is not null)
            Guard.Against.StringTooLong(description, 1000, nameof(description));

        Guard.Against.EnumOutOfRange(category, nameof(category));

        DisplayName = displayName.Trim();
        Description = description?.Trim();
        Category = category;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Publica o pack com a versão especificada.
    /// Transiciona o estado para Published e regista a versão corrente.
    /// </summary>
    /// <param name="version">Versão a publicar (máx. 50 caracteres, ex: "1.0.0").</param>
    public void Publish(string version)
    {
        Guard.Against.NullOrWhiteSpace(version, nameof(version));
        Guard.Against.StringTooLong(version, 50, nameof(version));

        Status = GovernancePackStatus.Published;
        CurrentVersion = version.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Deprecia o pack, sinalizando que não deve ser adotado em novos contextos.
    /// Packs já aplicados continuam válidos até substituição ou remoção explícita.
    /// </summary>
    public void Deprecate()
    {
        Status = GovernancePackStatus.Deprecated;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Arquiva o pack permanentemente.
    /// Mantido apenas para histórico e auditoria — não pode ser redistribuído.
    /// </summary>
    public void Archive()
    {
        Status = GovernancePackStatus.Archived;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
