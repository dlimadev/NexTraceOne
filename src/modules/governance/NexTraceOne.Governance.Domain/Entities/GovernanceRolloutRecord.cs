using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade GovernanceRolloutRecord.
/// Garante que GovernanceRolloutRecordId nunca seja confundido com outro tipo de Guid no sistema.
/// </summary>
public sealed record GovernanceRolloutRecordId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Regista a aplicação (rollout) de uma versão de governance pack a um escopo
/// específico. Permite rastrear quem iniciou a aplicação, quando, em que modo
/// de enforcement e qual o resultado — suportando auditoria completa,
/// rollback e análise de blast radius de alterações de governança.
/// </summary>
public sealed class GovernanceRolloutRecord : Entity<GovernanceRolloutRecordId>
{
    /// <summary>
    /// Identificador do governance pack aplicado.
    /// </summary>
    public GovernancePackId PackId { get; private init; } = default!;

    /// <summary>
    /// Identificador da versão específica do pack aplicada.
    /// </summary>
    public GovernancePackVersionId VersionId { get; private init; } = default!;

    /// <summary>
    /// Escopo ao qual o pack foi aplicado (ex: nome da equipa, domínio, ambiente).
    /// </summary>
    public string Scope { get; private init; } = string.Empty;

    /// <summary>
    /// Tipo de escopo do rollout, determinando o nível de aplicação.
    /// </summary>
    public GovernanceScopeType ScopeType { get; private init; }

    /// <summary>
    /// Modo de enforcement utilizado neste rollout.
    /// </summary>
    public EnforcementMode EnforcementMode { get; private init; }

    /// <summary>
    /// Estado atual do rollout.
    /// </summary>
    public RolloutStatus Status { get; private set; }

    /// <summary>
    /// Identificador do utilizador que iniciou o rollout.
    /// </summary>
    public string InitiatedBy { get; private init; } = string.Empty;

    /// <summary>Data/hora UTC em que o rollout foi iniciado.</summary>
    public DateTimeOffset InitiatedAt { get; private init; }

    /// <summary>
    /// Data/hora UTC em que o rollout foi concluído (sucesso, falha ou rollback).
    /// Null enquanto o rollout estiver em progresso.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Construtor privado para EF Core e serialização.</summary>
    private GovernanceRolloutRecord() { }

    /// <summary>
    /// Cria um novo registo de rollout com validação de invariantes.
    /// O estado inicial é Pending, aguardando execução.
    /// </summary>
    /// <param name="packId">Identificador do governance pack.</param>
    /// <param name="versionId">Identificador da versão do pack.</param>
    /// <param name="scope">Escopo de aplicação (máx. 200 caracteres).</param>
    /// <param name="scopeType">Tipo de escopo do rollout.</param>
    /// <param name="enforcementMode">Modo de enforcement utilizado.</param>
    /// <param name="initiatedBy">Identificador do utilizador que iniciou (máx. 200 caracteres).</param>
    /// <returns>Nova instância válida de GovernanceRolloutRecord.</returns>
    public static GovernanceRolloutRecord Create(
        GovernancePackId packId,
        GovernancePackVersionId versionId,
        string scope,
        GovernanceScopeType scopeType,
        EnforcementMode enforcementMode,
        string initiatedBy)
    {
        Guard.Against.Null(packId, nameof(packId));
        Guard.Against.Null(versionId, nameof(versionId));
        Guard.Against.NullOrWhiteSpace(scope, nameof(scope));
        Guard.Against.StringTooLong(scope, 200, nameof(scope));
        Guard.Against.EnumOutOfRange(scopeType, nameof(scopeType));
        Guard.Against.EnumOutOfRange(enforcementMode, nameof(enforcementMode));
        Guard.Against.NullOrWhiteSpace(initiatedBy, nameof(initiatedBy));
        Guard.Against.StringTooLong(initiatedBy, 200, nameof(initiatedBy));

        return new GovernanceRolloutRecord
        {
            Id = new GovernanceRolloutRecordId(Guid.NewGuid()),
            PackId = packId,
            VersionId = versionId,
            Scope = scope.Trim(),
            ScopeType = scopeType,
            EnforcementMode = enforcementMode,
            Status = RolloutStatus.Pending,
            InitiatedBy = initiatedBy.Trim(),
            InitiatedAt = DateTimeOffset.UtcNow,
            CompletedAt = null
        };
    }

    /// <summary>
    /// Marca o rollout como concluído com sucesso.
    /// </summary>
    public void MarkCompleted()
    {
        Status = RolloutStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marca o rollout como falhado.
    /// </summary>
    public void MarkFailed()
    {
        Status = RolloutStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Reverte o rollout, sinalizando que a versão aplicada foi removida do escopo.
    /// </summary>
    public void Rollback()
    {
        Status = RolloutStatus.RolledBack;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
