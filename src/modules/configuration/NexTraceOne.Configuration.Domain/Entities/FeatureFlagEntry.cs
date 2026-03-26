using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade FeatureFlagEntry.
/// Garante que FeatureFlagEntryId nunca seja confundido com outro tipo de Guid no sistema.
/// </summary>
public sealed record FeatureFlagEntryId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Entidade que representa uma substituição de valor de feature flag para um âmbito específico.
/// Cada entrada associa um valor booleano a uma definição de flag, num âmbito determinado,
/// permitindo que o valor padrão da definição seja sobreposto por âmbito
/// (ex: flag ativada globalmente mas desativada para um tenant específico).
///
/// A resolução hierárquica percorre User → Team → Role → Environment → Tenant → System
/// e devolve o primeiro valor efetivo encontrado, ou o DefaultEnabled da definição.
/// </summary>
public sealed class FeatureFlagEntry : Entity<FeatureFlagEntryId>
{
    /// <summary>
    /// Referência à definição da feature flag.
    /// </summary>
    public FeatureFlagDefinitionId DefinitionId { get; private init; } = default!;

    /// <summary>
    /// Chave da feature flag — desnormalizada para consultas rápidas sem join.
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
    /// Valor da substituição — verdadeiro se a flag está ativada para este âmbito, falso se desativada.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Indica se esta entrada está ativa e deve ser considerada na resolução de flags.
    /// Entradas inativas são ignoradas na resolução; o sistema recorre ao âmbito superior ou ao padrão.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Motivo da última alteração — utilizado para auditoria e rastreabilidade.
    /// </summary>
    public string? ChangeReason { get; private set; }

    /// <summary>Data/hora UTC de criação da entrada.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Identificador do utilizador que criou a entrada.</summary>
    public string CreatedBy { get; private init; } = string.Empty;

    /// <summary>Data/hora UTC da última atualização da entrada.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Identificador do utilizador que realizou a última atualização.</summary>
    public string? UpdatedBy { get; private set; }

    /// <summary>
    /// Token de concorrência otimista (PostgreSQL xmin).
    /// Utilizado pelo EF Core para detetar conflitos de escrita concorrente.
    /// </summary>
    public uint RowVersion { get; set; }

    /// <summary>Construtor privado para EF Core e serialização.</summary>
    private FeatureFlagEntry() { }

    /// <summary>
    /// Cria uma nova substituição de feature flag para um âmbito específico.
    /// </summary>
    /// <param name="definitionId">Referência à definição da feature flag.</param>
    /// <param name="key">Chave da feature flag (máx. 256 caracteres).</param>
    /// <param name="scope">Âmbito de aplicação.</param>
    /// <param name="isEnabled">Valor da substituição.</param>
    /// <param name="createdBy">Identificador do utilizador criador (máx. 200 caracteres).</param>
    /// <param name="scopeReferenceId">Identificador da entidade do âmbito (máx. 256 caracteres).</param>
    /// <param name="changeReason">Motivo da criação (máx. 500 caracteres).</param>
    /// <returns>Nova instância válida de FeatureFlagEntry.</returns>
    public static FeatureFlagEntry Create(
        FeatureFlagDefinitionId definitionId,
        string key,
        ConfigurationScope scope,
        bool isEnabled,
        string createdBy,
        string? scopeReferenceId = null,
        string? changeReason = null)
    {
        Guard.Against.Null(definitionId, nameof(definitionId));
        Guard.Against.NullOrWhiteSpace(key, nameof(key));
        Guard.Against.StringTooLong(key, 256, nameof(key));
        Guard.Against.NullOrWhiteSpace(createdBy, nameof(createdBy));
        Guard.Against.StringTooLong(createdBy, 200, nameof(createdBy));

        if (scopeReferenceId is not null)
            Guard.Against.StringTooLong(scopeReferenceId, 256, nameof(scopeReferenceId));

        if (changeReason is not null)
            Guard.Against.StringTooLong(changeReason, 500, nameof(changeReason));

        return new FeatureFlagEntry
        {
            Id = new FeatureFlagEntryId(Guid.NewGuid()),
            DefinitionId = definitionId,
            Key = key.Trim(),
            Scope = scope,
            ScopeReferenceId = scopeReferenceId?.Trim(),
            IsEnabled = isEnabled,
            IsActive = true,
            ChangeReason = changeReason?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy.Trim()
        };
    }

    /// <summary>
    /// Atualiza o valor da substituição de feature flag.
    /// </summary>
    /// <param name="isEnabled">Novo valor da flag.</param>
    /// <param name="updatedBy">Identificador do utilizador que realiza a atualização (máx. 200 caracteres).</param>
    /// <param name="changeReason">Motivo da alteração (máx. 500 caracteres).</param>
    public void UpdateValue(bool isEnabled, string updatedBy, string? changeReason = null)
    {
        Guard.Against.NullOrWhiteSpace(updatedBy, nameof(updatedBy));
        Guard.Against.StringTooLong(updatedBy, 200, nameof(updatedBy));

        if (changeReason is not null)
            Guard.Against.StringTooLong(changeReason, 500, nameof(changeReason));

        IsEnabled = isEnabled;
        ChangeReason = changeReason?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy.Trim();
    }

    /// <summary>
    /// Ativa a entrada, tornando-a elegível para resolução de feature flags.
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
    /// Desativa a entrada, removendo-a da resolução de feature flags sem a eliminar.
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
