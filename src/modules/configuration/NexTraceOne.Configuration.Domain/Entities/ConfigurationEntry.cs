using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade ConfigurationEntry.
/// Garante que ConfigurationEntryId nunca seja confundido com outro tipo de Guid no sistema.
/// </summary>
public sealed record ConfigurationEntryId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Agregado que representa um valor concreto de configuração armazenado na plataforma.
/// Cada entrada associa um valor a uma definição, num âmbito específico,
/// com controlo de versão, encriptação, janela de vigência e auditoria completa.
/// As entradas permitem sobreposição hierárquica de valores conforme o âmbito.
/// </summary>
public sealed class ConfigurationEntry : Entity<ConfigurationEntryId>
{
    /// <summary>
    /// Referência à definição que descreve os metadados desta configuração.
    /// </summary>
    public ConfigurationDefinitionId DefinitionId { get; private init; } = default!;

    /// <summary>
    /// Chave da configuração — desnormalizada para consultas rápidas sem join.
    /// </summary>
    public string Key { get; private init; } = string.Empty;

    /// <summary>
    /// Âmbito em que este valor se aplica (System, Tenant, Environment, Role, Team, User).
    /// </summary>
    public ConfigurationScope Scope { get; private init; }

    /// <summary>
    /// Identificador da entidade referenciada pelo âmbito (ex: TenantId, UserId, TeamId).
    /// Nulo quando o âmbito é System.
    /// </summary>
    public string? ScopeReferenceId { get; private init; }

    /// <summary>
    /// Valor da configuração em formato textual.
    /// </summary>
    public string? Value { get; private set; }

    /// <summary>
    /// Valor estruturado em JSON — utilizado quando o tipo da configuração é Json.
    /// Permite armazenamento e consulta de configurações complexas.
    /// </summary>
    public string? StructuredValueJson { get; private set; }

    /// <summary>
    /// Indica se o valor é sensível e deve ser mascarado na interface e nos logs.
    /// </summary>
    public bool IsSensitive { get; private init; }

    /// <summary>
    /// Indica se o valor está encriptado em repouso.
    /// </summary>
    public bool IsEncrypted { get; private set; }

    /// <summary>
    /// Versão do valor — incrementada a cada atualização para controlo de concorrência.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// Indica se esta entrada está ativa e deve ser considerada na resolução de configurações.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Motivo da última alteração — utilizado para auditoria e rastreabilidade.
    /// </summary>
    public string? ChangeReason { get; private set; }

    /// <summary>
    /// Data/hora UTC a partir da qual este valor é efetivo.
    /// Nulo significa efetivo imediatamente.
    /// </summary>
    public DateTimeOffset? EffectiveFrom { get; private set; }

    /// <summary>
    /// Data/hora UTC até à qual este valor é efetivo.
    /// Nulo significa sem data de expiração.
    /// </summary>
    public DateTimeOffset? EffectiveTo { get; private set; }

    /// <summary>Data/hora UTC de criação da entrada.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Identificador do utilizador que criou a entrada.</summary>
    public string CreatedBy { get; private init; } = string.Empty;

    /// <summary>Data/hora UTC da última atualização da entrada.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Identificador do utilizador que realizou a última atualização.</summary>
    public string? UpdatedBy { get; private set; }

    /// <summary>Construtor privado para EF Core e serialização.</summary>
    private ConfigurationEntry() { }

    /// <summary>
    /// Cria uma nova entrada de configuração com validação de invariantes.
    /// A entrada é criada como ativa na versão 1.
    /// </summary>
    /// <param name="definitionId">Referência à definição de configuração.</param>
    /// <param name="key">Chave da configuração (máx. 256 caracteres).</param>
    /// <param name="scope">Âmbito de aplicação.</param>
    /// <param name="createdBy">Identificador do utilizador criador (máx. 200 caracteres).</param>
    /// <param name="scopeReferenceId">Identificador da entidade do âmbito (máx. 256 caracteres).</param>
    /// <param name="value">Valor textual (máx. 4000 caracteres).</param>
    /// <param name="structuredValueJson">Valor JSON estruturado (máx. 8000 caracteres).</param>
    /// <param name="isSensitive">Indica se o valor é sensível.</param>
    /// <param name="isEncrypted">Indica se o valor está encriptado.</param>
    /// <param name="changeReason">Motivo da criação (máx. 500 caracteres).</param>
    /// <param name="effectiveFrom">Data de início de vigência.</param>
    /// <param name="effectiveTo">Data de fim de vigência.</param>
    /// <returns>Nova instância válida de ConfigurationEntry.</returns>
    public static ConfigurationEntry Create(
        ConfigurationDefinitionId definitionId,
        string key,
        ConfigurationScope scope,
        string createdBy,
        string? scopeReferenceId = null,
        string? value = null,
        string? structuredValueJson = null,
        bool isSensitive = false,
        bool isEncrypted = false,
        string? changeReason = null,
        DateTimeOffset? effectiveFrom = null,
        DateTimeOffset? effectiveTo = null)
    {
        Guard.Against.Null(definitionId, nameof(definitionId));
        Guard.Against.NullOrWhiteSpace(key, nameof(key));
        Guard.Against.StringTooLong(key, 256, nameof(key));
        Guard.Against.NullOrWhiteSpace(createdBy, nameof(createdBy));
        Guard.Against.StringTooLong(createdBy, 200, nameof(createdBy));

        if (scopeReferenceId is not null)
            Guard.Against.StringTooLong(scopeReferenceId, 256, nameof(scopeReferenceId));

        if (value is not null)
            Guard.Against.StringTooLong(value, 4000, nameof(value));

        if (structuredValueJson is not null)
            Guard.Against.StringTooLong(structuredValueJson, 8000, nameof(structuredValueJson));

        if (changeReason is not null)
            Guard.Against.StringTooLong(changeReason, 500, nameof(changeReason));

        return new ConfigurationEntry
        {
            Id = new ConfigurationEntryId(Guid.NewGuid()),
            DefinitionId = definitionId,
            Key = key.Trim(),
            Scope = scope,
            ScopeReferenceId = scopeReferenceId?.Trim(),
            Value = value?.Trim(),
            StructuredValueJson = structuredValueJson?.Trim(),
            IsSensitive = isSensitive,
            IsEncrypted = isEncrypted,
            Version = 1,
            IsActive = true,
            ChangeReason = changeReason?.Trim(),
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy.Trim()
        };
    }

    /// <summary>
    /// Atualiza o valor da entrada de configuração e incrementa a versão.
    /// </summary>
    /// <param name="value">Novo valor textual (máx. 4000 caracteres).</param>
    /// <param name="structuredValueJson">Novo valor JSON estruturado (máx. 8000 caracteres).</param>
    /// <param name="updatedBy">Identificador do utilizador que realiza a atualização (máx. 200 caracteres).</param>
    /// <param name="changeReason">Motivo da alteração (máx. 500 caracteres).</param>
    /// <param name="isEncrypted">Indica se o novo valor está encriptado.</param>
    /// <param name="effectiveFrom">Nova data de início de vigência.</param>
    /// <param name="effectiveTo">Nova data de fim de vigência.</param>
    public void UpdateValue(
        string? value,
        string? structuredValueJson,
        string updatedBy,
        string? changeReason = null,
        bool? isEncrypted = null,
        DateTimeOffset? effectiveFrom = null,
        DateTimeOffset? effectiveTo = null)
    {
        Guard.Against.NullOrWhiteSpace(updatedBy, nameof(updatedBy));
        Guard.Against.StringTooLong(updatedBy, 200, nameof(updatedBy));

        if (value is not null)
            Guard.Against.StringTooLong(value, 4000, nameof(value));

        if (structuredValueJson is not null)
            Guard.Against.StringTooLong(structuredValueJson, 8000, nameof(structuredValueJson));

        if (changeReason is not null)
            Guard.Against.StringTooLong(changeReason, 500, nameof(changeReason));

        Value = value?.Trim();
        StructuredValueJson = structuredValueJson?.Trim();
        ChangeReason = changeReason?.Trim();
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Version++;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy.Trim();

        if (isEncrypted.HasValue)
            IsEncrypted = isEncrypted.Value;
    }

    /// <summary>
    /// Ativa a entrada, tornando-a elegível para resolução de configurações.
    /// </summary>
    /// <param name="updatedBy">Identificador do utilizador que realiza a ativação (máx. 200 caracteres).</param>
    /// <param name="changeReason">Motivo da ativação (máx. 500 caracteres).</param>
    public void Activate(string updatedBy, string? changeReason = null)
    {
        Guard.Against.NullOrWhiteSpace(updatedBy, nameof(updatedBy));
        Guard.Against.StringTooLong(updatedBy, 200, nameof(updatedBy));

        if (changeReason is not null)
            Guard.Against.StringTooLong(changeReason, 500, nameof(changeReason));

        IsActive = true;
        ChangeReason = changeReason?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy.Trim();
    }

    /// <summary>
    /// Desativa a entrada, removendo-a da resolução de configurações sem a eliminar.
    /// </summary>
    /// <param name="updatedBy">Identificador do utilizador que realiza a desativação (máx. 200 caracteres).</param>
    /// <param name="changeReason">Motivo da desativação (máx. 500 caracteres).</param>
    public void Deactivate(string updatedBy, string? changeReason = null)
    {
        Guard.Against.NullOrWhiteSpace(updatedBy, nameof(updatedBy));
        Guard.Against.StringTooLong(updatedBy, 200, nameof(updatedBy));

        if (changeReason is not null)
            Guard.Against.StringTooLong(changeReason, 500, nameof(changeReason));

        IsActive = false;
        ChangeReason = changeReason?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy.Trim();
    }
}
