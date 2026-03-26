using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade ConfigurationModule.
/// Garante que ConfigurationModuleId nunca seja confundido com outro tipo de Guid no sistema.
/// </summary>
public sealed record ConfigurationModuleId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Entidade que representa um módulo ou domínio funcional da plataforma no contexto
/// da hierarquia de configuração. Permite agrupar definições de configuração por
/// área de responsabilidade (ex: "notifications", "ai", "governance", "contracts").
/// Torna explícita a dimensão "módulo" da hierarquia Instance → Tenant → Environment → Module,
/// substituindo a convenção implícita de prefixo de chave (ex: "notifications.*").
/// </summary>
public sealed class ConfigurationModule : Entity<ConfigurationModuleId>
{
    /// <summary>
    /// Chave única e imutável do módulo (ex: "notifications", "ai", "governance").
    /// Utilizada como referência estável em agrupamentos e pesquisas.
    /// </summary>
    public string Key { get; private init; } = string.Empty;

    /// <summary>
    /// Nome de exibição legível do módulo para interfaces de gestão.
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Descrição do propósito e escopo do módulo.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Ordem de apresentação na interface — permite agrupamento e ordenação consistente.
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Indica se o módulo está ativo e deve ser exibido nas interfaces de administração.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>Data/hora UTC de criação do módulo.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última atualização do módulo.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>
    /// Token de concorrência otimista (PostgreSQL xmin).
    /// Utilizado pelo EF Core para detetar conflitos de escrita concorrente.
    /// </summary>
    public uint RowVersion { get; set; }

    /// <summary>Construtor privado para EF Core e serialização.</summary>
    private ConfigurationModule() { }

    /// <summary>
    /// Cria um novo módulo de configuração com validação de invariantes.
    /// A chave é obrigatória, única e imutável após criação.
    /// </summary>
    /// <param name="key">Chave única do módulo (máx. 100 caracteres).</param>
    /// <param name="displayName">Nome de exibição (máx. 200 caracteres).</param>
    /// <param name="description">Descrição opcional (máx. 500 caracteres).</param>
    /// <param name="sortOrder">Ordem de apresentação.</param>
    /// <returns>Nova instância válida de ConfigurationModule.</returns>
    public static ConfigurationModule Create(
        string key,
        string displayName,
        string? description = null,
        int sortOrder = 0)
    {
        Guard.Against.NullOrWhiteSpace(key, nameof(key));
        Guard.Against.StringTooLong(key, 100, nameof(key));
        Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
        Guard.Against.StringTooLong(displayName, 200, nameof(displayName));

        if (description is not null)
            Guard.Against.StringTooLong(description, 500, nameof(description));

        return new ConfigurationModule
        {
            Id = new ConfigurationModuleId(Guid.NewGuid()),
            Key = key.Trim().ToLowerInvariant(),
            DisplayName = displayName.Trim(),
            Description = description?.Trim(),
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Atualiza os metadados mutáveis do módulo.
    /// A chave não pode ser alterada após criação.
    /// </summary>
    /// <param name="displayName">Novo nome de exibição (máx. 200 caracteres).</param>
    /// <param name="description">Nova descrição opcional (máx. 500 caracteres).</param>
    /// <param name="sortOrder">Nova ordem de apresentação.</param>
    public void Update(string displayName, string? description, int sortOrder)
    {
        Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
        Guard.Against.StringTooLong(displayName, 200, nameof(displayName));

        if (description is not null)
            Guard.Against.StringTooLong(description, 500, nameof(description));

        DisplayName = displayName.Trim();
        Description = description?.Trim();
        SortOrder = sortOrder;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Ativa o módulo, tornando-o visível nas interfaces de administração.</summary>
    public void Activate() { IsActive = true; UpdatedAt = DateTimeOffset.UtcNow; }

    /// <summary>Desativa o módulo, ocultando-o das interfaces de administração.</summary>
    public void Deactivate() { IsActive = false; UpdatedAt = DateTimeOffset.UtcNow; }
}
