using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade ConfigurationDefinition.
/// Garante que ConfigurationDefinitionId nunca seja confundido com outro tipo de Guid no sistema.
/// </summary>
public sealed record ConfigurationDefinitionId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Agregado que representa a definição e metadados de uma chave de configuração.
/// Cada definição descreve o schema, tipo, regras de validação e comportamento
/// de uma configuração dentro da plataforma. A definição é a fonte de verdade
/// sobre o que uma configuração significa, quais âmbitos são permitidos
/// e como o valor deve ser validado e apresentado.
/// </summary>
public sealed class ConfigurationDefinition : Entity<ConfigurationDefinitionId>
{
    /// <summary>
    /// Chave única e imutável da configuração (ex: "platform.notifications.enabled").
    /// Utilizada como referência estável em todo o sistema.
    /// </summary>
    public string Key { get; private init; } = string.Empty;

    /// <summary>
    /// Nome de exibição legível da configuração para interfaces de gestão.
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Descrição detalhada do propósito e impacto da configuração.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Categoria da configuração — controla ciclo de vida e regras de governança.
    /// </summary>
    public ConfigurationCategory Category { get; private set; }

    /// <summary>
    /// Âmbitos nos quais esta configuração pode ser definida.
    /// Armazenado como array para permitir múltiplos âmbitos por definição.
    /// </summary>
    public ConfigurationScope[] AllowedScopes { get; private set; } = [];

    /// <summary>
    /// Valor padrão da configuração, utilizado quando nenhum valor específico está definido.
    /// </summary>
    public string? DefaultValue { get; private set; }

    /// <summary>
    /// Tipo do valor da configuração — determina validação e editor na interface.
    /// </summary>
    public ConfigurationValueType ValueType { get; private set; }

    /// <summary>
    /// Indica se o valor é sensível e deve ser mascarado na interface e nos logs.
    /// </summary>
    public bool IsSensitive { get; private set; }

    /// <summary>
    /// Indica se a configuração pode ser editada por utilizadores autorizados.
    /// Configurações não editáveis só podem ser alteradas por processos internos.
    /// </summary>
    public bool IsEditable { get; private set; }

    /// <summary>
    /// Indica se o valor pode ser herdado de âmbitos superiores na hierarquia.
    /// </summary>
    public bool IsInheritable { get; private set; }

    /// <summary>
    /// Regras de validação em formato JSON (ex: min/max, regex, valores permitidos).
    /// Aplicadas na criação e atualização de valores.
    /// </summary>
    public string? ValidationRules { get; private set; }

    /// <summary>
    /// Tipo de editor na interface de administração (ex: "text", "toggle", "json-editor", "select").
    /// Permite personalizar a experiência de edição conforme o tipo de configuração.
    /// </summary>
    public string? UiEditorType { get; private set; }

    /// <summary>
    /// Ordem de apresentação na interface — permite agrupamento e ordenação consistente.
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>Data/hora UTC de criação da definição.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última atualização da definição.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Construtor privado para EF Core e serialização.</summary>
    private ConfigurationDefinition() { }

    /// <summary>
    /// Cria uma nova definição de configuração com validação de invariantes.
    /// A chave é obrigatória, única e imutável após criação.
    /// </summary>
    /// <param name="key">Chave única da configuração (máx. 256 caracteres).</param>
    /// <param name="displayName">Nome de exibição (máx. 200 caracteres).</param>
    /// <param name="category">Categoria da configuração.</param>
    /// <param name="valueType">Tipo do valor.</param>
    /// <param name="allowedScopes">Âmbitos permitidos para esta configuração.</param>
    /// <param name="description">Descrição opcional (máx. 1000 caracteres).</param>
    /// <param name="defaultValue">Valor padrão opcional (máx. 4000 caracteres).</param>
    /// <param name="isSensitive">Indica se o valor é sensível.</param>
    /// <param name="isEditable">Indica se é editável por utilizadores.</param>
    /// <param name="isInheritable">Indica se suporta herança hierárquica.</param>
    /// <param name="validationRules">Regras de validação em JSON opcional (máx. 4000 caracteres).</param>
    /// <param name="uiEditorType">Tipo de editor na UI opcional (máx. 100 caracteres).</param>
    /// <param name="sortOrder">Ordem de apresentação.</param>
    /// <returns>Nova instância válida de ConfigurationDefinition.</returns>
    public static ConfigurationDefinition Create(
        string key,
        string displayName,
        ConfigurationCategory category,
        ConfigurationValueType valueType,
        ConfigurationScope[] allowedScopes,
        string? description = null,
        string? defaultValue = null,
        bool isSensitive = false,
        bool isEditable = true,
        bool isInheritable = true,
        string? validationRules = null,
        string? uiEditorType = null,
        int sortOrder = 0)
    {
        Guard.Against.NullOrWhiteSpace(key, nameof(key));
        Guard.Against.StringTooLong(key, 256, nameof(key));
        Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
        Guard.Against.StringTooLong(displayName, 200, nameof(displayName));
        Guard.Against.Null(allowedScopes, nameof(allowedScopes));
        Guard.Against.Zero(allowedScopes.Length, nameof(allowedScopes));

        if (description is not null)
            Guard.Against.StringTooLong(description, 1000, nameof(description));

        if (defaultValue is not null)
            Guard.Against.StringTooLong(defaultValue, 4000, nameof(defaultValue));

        if (validationRules is not null)
            Guard.Against.StringTooLong(validationRules, 4000, nameof(validationRules));

        if (uiEditorType is not null)
            Guard.Against.StringTooLong(uiEditorType, 100, nameof(uiEditorType));

        return new ConfigurationDefinition
        {
            Id = new ConfigurationDefinitionId(Guid.NewGuid()),
            Key = key.Trim(),
            DisplayName = displayName.Trim(),
            Description = description?.Trim(),
            Category = category,
            AllowedScopes = allowedScopes,
            DefaultValue = defaultValue?.Trim(),
            ValueType = valueType,
            IsSensitive = isSensitive,
            IsEditable = isEditable,
            IsInheritable = isInheritable,
            ValidationRules = validationRules?.Trim(),
            UiEditorType = uiEditorType?.Trim(),
            SortOrder = sortOrder,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Atualiza os metadados mutáveis da definição de configuração.
    /// A chave não pode ser alterada após criação.
    /// </summary>
    /// <param name="displayName">Novo nome de exibição (máx. 200 caracteres).</param>
    /// <param name="description">Nova descrição opcional (máx. 1000 caracteres).</param>
    /// <param name="allowedScopes">Novos âmbitos permitidos.</param>
    /// <param name="defaultValue">Novo valor padrão opcional (máx. 4000 caracteres).</param>
    /// <param name="isSensitive">Indica se o valor é sensível.</param>
    /// <param name="isEditable">Indica se é editável.</param>
    /// <param name="isInheritable">Indica se suporta herança.</param>
    /// <param name="validationRules">Novas regras de validação em JSON (máx. 4000 caracteres).</param>
    /// <param name="uiEditorType">Novo tipo de editor na UI (máx. 100 caracteres).</param>
    /// <param name="sortOrder">Nova ordem de apresentação.</param>
    public void Update(
        string displayName,
        string? description,
        ConfigurationScope[] allowedScopes,
        string? defaultValue,
        bool isSensitive,
        bool isEditable,
        bool isInheritable,
        string? validationRules,
        string? uiEditorType,
        int sortOrder)
    {
        Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
        Guard.Against.StringTooLong(displayName, 200, nameof(displayName));
        Guard.Against.Null(allowedScopes, nameof(allowedScopes));
        Guard.Against.Zero(allowedScopes.Length, nameof(allowedScopes));

        if (description is not null)
            Guard.Against.StringTooLong(description, 1000, nameof(description));

        if (defaultValue is not null)
            Guard.Against.StringTooLong(defaultValue, 4000, nameof(defaultValue));

        if (validationRules is not null)
            Guard.Against.StringTooLong(validationRules, 4000, nameof(validationRules));

        if (uiEditorType is not null)
            Guard.Against.StringTooLong(uiEditorType, 100, nameof(uiEditorType));

        DisplayName = displayName.Trim();
        Description = description?.Trim();
        AllowedScopes = allowedScopes;
        DefaultValue = defaultValue?.Trim();
        IsSensitive = isSensitive;
        IsEditable = isEditable;
        IsInheritable = isInheritable;
        ValidationRules = validationRules?.Trim();
        UiEditorType = uiEditorType?.Trim();
        SortOrder = sortOrder;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
