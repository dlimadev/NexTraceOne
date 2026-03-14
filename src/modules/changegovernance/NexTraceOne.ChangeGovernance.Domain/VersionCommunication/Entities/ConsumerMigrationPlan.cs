using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.VersionCommunication.Domain.Enums;
using NexTraceOne.VersionCommunication.Domain.Errors;

namespace NexTraceOne.VersionCommunication.Domain.Entities;

/// <summary>
/// Entidade que rastreia o progresso de migração de um consumidor individual
/// dentro de um plano de rollout de versão. Cada consumidor identificado no
/// blast radius do rollout recebe seu próprio plano de migração, permitindo
/// acompanhar notificação, reconhecimento e conclusão da migração por consumidor.
/// </summary>
public sealed class ConsumerMigrationPlan : AuditableEntity<ConsumerMigrationPlanId>
{
    private ConsumerMigrationPlan() { }

    /// <summary>Identificador do plano de rollout ao qual este plano de migração pertence.</summary>
    public VersionRolloutPlanId VersionRolloutPlanId { get; private set; } = null!;

    /// <summary>Identificador do ativo consumidor no módulo Catalog.</summary>
    public Guid ConsumerAssetId { get; private set; }

    /// <summary>Nome legível do consumidor para exibição em dashboards e relatórios.</summary>
    public string ConsumerName { get; private set; } = string.Empty;

    /// <summary>Status atual do processo de migração deste consumidor.</summary>
    public ConsumerMigrationStatus Status { get; private set; } = ConsumerMigrationStatus.Pending;

    /// <summary>Data/hora UTC em que o consumidor foi notificado sobre a migração.</summary>
    public DateTimeOffset? NotifiedAt { get; private set; }

    /// <summary>Data/hora UTC em que o consumidor confirmou ciência da migração.</summary>
    public DateTimeOffset? AcknowledgedAt { get; private set; }

    /// <summary>Data/hora UTC em que o consumidor completou a migração para a nova versão.</summary>
    public DateTimeOffset? MigratedAt { get; private set; }

    /// <summary>Observações adicionais sobre o progresso de migração do consumidor.</summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Cria um novo plano de migração para um consumidor específico dentro de um rollout de versão.
    /// O plano inicia no status Pending, aguardando notificação ao consumidor.
    /// </summary>
    public static ConsumerMigrationPlan Create(
        VersionRolloutPlanId versionRolloutPlanId,
        Guid consumerAssetId,
        string consumerName,
        string? notes = null)
    {
        Guard.Against.Null(versionRolloutPlanId);
        Guard.Against.Default(consumerAssetId);
        Guard.Against.NullOrWhiteSpace(consumerName);

        return new ConsumerMigrationPlan
        {
            Id = ConsumerMigrationPlanId.New(),
            VersionRolloutPlanId = versionRolloutPlanId,
            ConsumerAssetId = consumerAssetId,
            ConsumerName = consumerName,
            Status = ConsumerMigrationStatus.Pending,
            Notes = notes
        };
    }

    /// <summary>
    /// Marca o consumidor como notificado sobre a migração pendente.
    /// Transição: Pending → Notified.
    /// Retorna falha se a transição de status for inválida.
    /// </summary>
    public Result<Unit> MarkNotified(DateTimeOffset at)
    {
        var result = TransitionTo(ConsumerMigrationStatus.Notified);
        if (result.IsFailure)
            return result;

        NotifiedAt = at;
        return Unit.Value;
    }

    /// <summary>
    /// Marca o consumidor como tendo reconhecido a necessidade de migração.
    /// Transição: Notified → Acknowledged.
    /// Retorna falha se a transição de status for inválida.
    /// </summary>
    public Result<Unit> MarkAcknowledged(DateTimeOffset at)
    {
        var result = TransitionTo(ConsumerMigrationStatus.Acknowledged);
        if (result.IsFailure)
            return result;

        AcknowledgedAt = at;
        return Unit.Value;
    }

    /// <summary>
    /// Marca o consumidor como tendo completado a migração para a nova versão.
    /// Transição: Acknowledged/InProgress → Completed.
    /// Retorna falha se a transição de status for inválida.
    /// </summary>
    public Result<Unit> MarkMigrated(DateTimeOffset at)
    {
        var result = TransitionTo(ConsumerMigrationStatus.Completed);
        if (result.IsFailure)
            return result;

        MigratedAt = at;
        return Unit.Value;
    }

    /// <summary>
    /// Inicia o processo ativo de migração do consumidor.
    /// Transição: Acknowledged → InProgress.
    /// Retorna falha se a transição de status for inválida.
    /// </summary>
    public Result<Unit> StartMigration()
        => TransitionTo(ConsumerMigrationStatus.InProgress);

    /// <summary>
    /// Ignora este consumidor no plano de migração (ex: consumidor descontinuado).
    /// Transição: Pending → Skipped.
    /// Retorna falha se o consumidor já estiver em estado terminal.
    /// </summary>
    public Result<Unit> Skip()
        => TransitionTo(ConsumerMigrationStatus.Skipped);

    /// <summary>
    /// Transição centralizada de status — valida a transição e atualiza o estado.
    /// </summary>
    private Result<Unit> TransitionTo(ConsumerMigrationStatus newStatus)
    {
        if (IsTerminal())
            return VersionCommunicationErrors.ConsumerMigrationAlreadyTerminal();

        if (!IsValidTransition(Status, newStatus))
            return VersionCommunicationErrors.InvalidConsumerMigrationTransition(
                Status.ToString(), newStatus.ToString());

        Status = newStatus;
        return Unit.Value;
    }

    /// <summary>
    /// Verifica se o plano de migração está em estado terminal (Completed ou Skipped).
    /// </summary>
    private bool IsTerminal()
        => Status is ConsumerMigrationStatus.Completed or ConsumerMigrationStatus.Skipped;

    /// <summary>
    /// Valida se a transição de status é permitida no ciclo de vida da migração.
    /// Transições válidas: Pending → Notified → Acknowledged → InProgress → Completed;
    /// Pending → Skipped; Acknowledged → Completed (migração direta sem fase InProgress).
    /// </summary>
    private static bool IsValidTransition(ConsumerMigrationStatus from, ConsumerMigrationStatus to) =>
        (from, to) switch
        {
            (ConsumerMigrationStatus.Pending, ConsumerMigrationStatus.Notified) => true,
            (ConsumerMigrationStatus.Pending, ConsumerMigrationStatus.Skipped) => true,
            (ConsumerMigrationStatus.Notified, ConsumerMigrationStatus.Acknowledged) => true,
            (ConsumerMigrationStatus.Acknowledged, ConsumerMigrationStatus.InProgress) => true,
            (ConsumerMigrationStatus.Acknowledged, ConsumerMigrationStatus.Completed) => true,
            (ConsumerMigrationStatus.InProgress, ConsumerMigrationStatus.Completed) => true,
            _ => false
        };
}

/// <summary>Identificador fortemente tipado de ConsumerMigrationPlan.</summary>
public sealed record ConsumerMigrationPlanId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ConsumerMigrationPlanId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ConsumerMigrationPlanId From(Guid id) => new(id);
}
