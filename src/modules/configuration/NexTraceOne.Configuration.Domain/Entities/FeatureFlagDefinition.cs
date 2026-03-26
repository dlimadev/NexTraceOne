using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade FeatureFlagDefinition.
/// Garante que FeatureFlagDefinitionId nunca seja confundido com outro tipo de Guid no sistema.
/// </summary>
public sealed record FeatureFlagDefinitionId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Entidade que define os metadados de uma feature flag da plataforma.
/// Representa a definição persistida de uma flag — o que ela é, quais âmbitos suporta,
/// qual é o valor padrão e a qual módulo pertence.
/// Esta entidade prepara o terreno para a resolução e avaliação de feature flags
/// por âmbito (Instance → Tenant → Environment) nas fases seguintes.
///
/// NOTA: A resolução de valores por âmbito (FeatureFlagEntry) e a UI de gestão
/// completa ficam para a fase P3.2.
/// </summary>
public sealed class FeatureFlagDefinition : Entity<FeatureFlagDefinitionId>
{
    /// <summary>
    /// Chave única e imutável da feature flag (ex: "ai.assistant.enabled").
    /// Utilizada como referência estável em todo o sistema.
    /// </summary>
    public string Key { get; private init; } = string.Empty;

    /// <summary>
    /// Nome de exibição legível da feature flag para interfaces de gestão.
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Descrição detalhada do propósito e impacto da feature flag.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Valor padrão da flag quando nenhuma substituição específica está definida.
    /// </summary>
    public bool DefaultEnabled { get; private set; }

    /// <summary>
    /// Âmbitos nos quais esta flag pode ser substituída.
    /// Armazenado como array para permitir múltiplos âmbitos por definição.
    /// </summary>
    public ConfigurationScope[] AllowedScopes { get; private set; } = [];

    /// <summary>
    /// Referência opcional ao módulo de configuração ao qual esta flag pertence.
    /// Permite agrupar flags por domínio funcional.
    /// </summary>
    public ConfigurationModuleId? ModuleId { get; private set; }

    /// <summary>
    /// Indica se a flag está ativa e disponível para avaliação.
    /// Flags inativas são tratadas como desativadas independentemente do valor.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Indica se a flag pode ser editada por administradores via interface.
    /// Flags não editáveis só podem ser alteradas por processos internos ou deploys.
    /// </summary>
    public bool IsEditable { get; private set; }

    /// <summary>Data/hora UTC de criação da definição.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última atualização da definição.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>
    /// Token de concorrência otimista (PostgreSQL xmin).
    /// Utilizado pelo EF Core para detetar conflitos de escrita concorrente.
    /// </summary>
    public uint RowVersion { get; set; }

    /// <summary>Construtor privado para EF Core e serialização.</summary>
    private FeatureFlagDefinition() { }

    /// <summary>
    /// Cria uma nova definição de feature flag com validação de invariantes.
    /// A chave é obrigatória, única e imutável após criação.
    /// </summary>
    /// <param name="key">Chave única da flag (máx. 256 caracteres).</param>
    /// <param name="displayName">Nome de exibição (máx. 200 caracteres).</param>
    /// <param name="allowedScopes">Âmbitos onde a flag pode ser substituída.</param>
    /// <param name="description">Descrição opcional (máx. 1000 caracteres).</param>
    /// <param name="defaultEnabled">Valor padrão da flag.</param>
    /// <param name="moduleId">Módulo ao qual a flag pertence (opcional).</param>
    /// <param name="isEditable">Indica se é editável por administradores.</param>
    /// <returns>Nova instância válida de FeatureFlagDefinition.</returns>
    public static FeatureFlagDefinition Create(
        string key,
        string displayName,
        ConfigurationScope[] allowedScopes,
        string? description = null,
        bool defaultEnabled = false,
        ConfigurationModuleId? moduleId = null,
        bool isEditable = true)
    {
        Guard.Against.NullOrWhiteSpace(key, nameof(key));
        Guard.Against.StringTooLong(key, 256, nameof(key));
        Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
        Guard.Against.StringTooLong(displayName, 200, nameof(displayName));
        Guard.Against.Null(allowedScopes, nameof(allowedScopes));
        Guard.Against.Zero(allowedScopes.Length, nameof(allowedScopes));

        if (description is not null)
            Guard.Against.StringTooLong(description, 1000, nameof(description));

        return new FeatureFlagDefinition
        {
            Id = new FeatureFlagDefinitionId(Guid.NewGuid()),
            Key = key.Trim(),
            DisplayName = displayName.Trim(),
            Description = description?.Trim(),
            DefaultEnabled = defaultEnabled,
            AllowedScopes = allowedScopes,
            ModuleId = moduleId,
            IsActive = true,
            IsEditable = isEditable,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Atualiza os metadados mutáveis da definição de feature flag.
    /// A chave não pode ser alterada após criação.
    /// </summary>
    /// <param name="displayName">Novo nome de exibição (máx. 200 caracteres).</param>
    /// <param name="description">Nova descrição opcional (máx. 1000 caracteres).</param>
    /// <param name="allowedScopes">Novos âmbitos permitidos.</param>
    /// <param name="defaultEnabled">Novo valor padrão.</param>
    /// <param name="moduleId">Novo módulo ao qual a flag pertence (opcional).</param>
    /// <param name="isEditable">Indica se é editável.</param>
    public void Update(
        string displayName,
        string? description,
        ConfigurationScope[] allowedScopes,
        bool defaultEnabled,
        ConfigurationModuleId? moduleId,
        bool isEditable)
    {
        Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
        Guard.Against.StringTooLong(displayName, 200, nameof(displayName));
        Guard.Against.Null(allowedScopes, nameof(allowedScopes));
        Guard.Against.Zero(allowedScopes.Length, nameof(allowedScopes));

        if (description is not null)
            Guard.Against.StringTooLong(description, 1000, nameof(description));

        DisplayName = displayName.Trim();
        Description = description?.Trim();
        AllowedScopes = allowedScopes;
        DefaultEnabled = defaultEnabled;
        ModuleId = moduleId;
        IsEditable = isEditable;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Ativa a definição de feature flag.</summary>
    public void Activate() { IsActive = true; UpdatedAt = DateTimeOffset.UtcNow; }

    /// <summary>Desativa a definição de feature flag.</summary>
    public void Deactivate() { IsActive = false; UpdatedAt = DateTimeOffset.UtcNow; }
}
