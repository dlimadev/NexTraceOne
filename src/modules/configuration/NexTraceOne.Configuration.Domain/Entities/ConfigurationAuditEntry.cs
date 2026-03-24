using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade ConfigurationAuditEntry.
/// Garante que ConfigurationAuditEntryId nunca seja confundido com outro tipo de Guid no sistema.
/// </summary>
public sealed record ConfigurationAuditEntryId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Registo de auditoria para alterações em configurações da plataforma.
/// Cada entrada de auditoria captura o estado anterior e novo de uma configuração,
/// o utilizador responsável, o motivo da alteração e metadados de rastreabilidade.
/// Valores sensíveis são identificados para controlo de mascaramento na interface.
/// </summary>
public sealed class ConfigurationAuditEntry : Entity<ConfigurationAuditEntryId>
{
    /// <summary>
    /// Referência à entrada de configuração que foi alterada.
    /// </summary>
    public ConfigurationEntryId EntryId { get; private init; } = default!;

    /// <summary>
    /// Chave da configuração — desnormalizada para consultas e relatórios de auditoria.
    /// </summary>
    public string Key { get; private init; } = string.Empty;

    /// <summary>
    /// Âmbito em que a alteração ocorreu.
    /// </summary>
    public ConfigurationScope Scope { get; private init; }

    /// <summary>
    /// Identificador da entidade referenciada pelo âmbito no momento da alteração.
    /// </summary>
    public string? ScopeReferenceId { get; private init; }

    /// <summary>
    /// Ação realizada sobre a configuração (ex: "Created", "Updated", "Activated", "Deactivated").
    /// </summary>
    public string Action { get; private init; } = string.Empty;

    /// <summary>
    /// Valor anterior à alteração — nulo quando a ação é "Created".
    /// </summary>
    public string? PreviousValue { get; private init; }

    /// <summary>
    /// Novo valor após a alteração — nulo quando a ação é "Deactivated".
    /// </summary>
    public string? NewValue { get; private init; }

    /// <summary>
    /// Versão anterior da entrada — nula quando a ação é "Created".
    /// </summary>
    public int? PreviousVersion { get; private init; }

    /// <summary>
    /// Versão da entrada após a alteração.
    /// </summary>
    public int NewVersion { get; private init; }

    /// <summary>
    /// Identificador do utilizador que realizou a alteração.
    /// </summary>
    public string ChangedBy { get; private init; } = string.Empty;

    /// <summary>
    /// Data/hora UTC em que a alteração ocorreu.
    /// </summary>
    public DateTimeOffset ChangedAt { get; private init; }

    /// <summary>
    /// Motivo da alteração — utilizado para rastreabilidade e compliance.
    /// </summary>
    public string? ChangeReason { get; private init; }

    /// <summary>
    /// Indica se os valores registados são sensíveis e devem ser mascarados na interface.
    /// </summary>
    public bool IsSensitive { get; private init; }

    /// <summary>Construtor privado para EF Core e serialização.</summary>
    private ConfigurationAuditEntry() { }

    /// <summary>
    /// Cria um novo registo de auditoria para uma alteração de configuração.
    /// </summary>
    /// <param name="entryId">Referência à entrada de configuração alterada.</param>
    /// <param name="key">Chave da configuração (máx. 256 caracteres).</param>
    /// <param name="scope">Âmbito da alteração.</param>
    /// <param name="action">Ação realizada (máx. 50 caracteres).</param>
    /// <param name="newVersion">Nova versão da entrada.</param>
    /// <param name="changedBy">Utilizador responsável pela alteração (máx. 200 caracteres).</param>
    /// <param name="scopeReferenceId">Identificador da entidade do âmbito (máx. 256 caracteres).</param>
    /// <param name="previousValue">Valor anterior (máx. 4000 caracteres).</param>
    /// <param name="newValue">Novo valor (máx. 4000 caracteres).</param>
    /// <param name="previousVersion">Versão anterior.</param>
    /// <param name="changeReason">Motivo da alteração (máx. 500 caracteres).</param>
    /// <param name="isSensitive">Indica se os valores são sensíveis.</param>
    /// <returns>Nova instância válida de ConfigurationAuditEntry.</returns>
    public static ConfigurationAuditEntry Create(
        ConfigurationEntryId entryId,
        string key,
        ConfigurationScope scope,
        string action,
        int newVersion,
        string changedBy,
        string? scopeReferenceId = null,
        string? previousValue = null,
        string? newValue = null,
        int? previousVersion = null,
        string? changeReason = null,
        bool isSensitive = false)
    {
        Guard.Against.Null(entryId, nameof(entryId));
        Guard.Against.NullOrWhiteSpace(key, nameof(key));
        Guard.Against.StringTooLong(key, 256, nameof(key));
        Guard.Against.NullOrWhiteSpace(action, nameof(action));
        Guard.Against.StringTooLong(action, 50, nameof(action));
        Guard.Against.NullOrWhiteSpace(changedBy, nameof(changedBy));
        Guard.Against.StringTooLong(changedBy, 200, nameof(changedBy));

        if (scopeReferenceId is not null)
            Guard.Against.StringTooLong(scopeReferenceId, 256, nameof(scopeReferenceId));

        if (previousValue is not null)
            Guard.Against.StringTooLong(previousValue, 4000, nameof(previousValue));

        if (newValue is not null)
            Guard.Against.StringTooLong(newValue, 4000, nameof(newValue));

        if (changeReason is not null)
            Guard.Against.StringTooLong(changeReason, 500, nameof(changeReason));

        return new ConfigurationAuditEntry
        {
            Id = new ConfigurationAuditEntryId(Guid.NewGuid()),
            EntryId = entryId,
            Key = key.Trim(),
            Scope = scope,
            ScopeReferenceId = scopeReferenceId?.Trim(),
            Action = action.Trim(),
            PreviousValue = previousValue?.Trim(),
            NewValue = newValue?.Trim(),
            PreviousVersion = previousVersion,
            NewVersion = newVersion,
            ChangedBy = changedBy.Trim(),
            ChangedAt = DateTimeOffset.UtcNow,
            ChangeReason = changeReason?.Trim(),
            IsSensitive = isSensitive
        };
    }
}
