using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.VersionCommunication.Domain.Enums;
using NexTraceOne.VersionCommunication.Domain.Errors;

namespace NexTraceOne.VersionCommunication.Domain.Entities;

/// <summary>
/// Aggregate Root que representa um plano de rollout de nova versão de API.
/// Orquestra o ciclo de vida completo da comunicação de versão: desde o anúncio
/// da nova versão, passando pela janela de migração dos consumidores, até a conclusão
/// ou cancelamento do plano. Garante que consumidores tenham visibilidade e tempo
/// adequado para migrar antes da deprecação da versão anterior.
/// </summary>
public sealed class VersionRolloutPlan : AggregateRoot<VersionRolloutPlanId>
{
    private VersionRolloutPlan() { }

    /// <summary>Identificador do tenant proprietário deste plano de rollout.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Identificador do ativo de API no módulo Catalog cuja versão será atualizada.</summary>
    public Guid ApiAssetId { get; private set; }

    /// <summary>Versão de origem (versão atual que será substituída).</summary>
    public string FromVersion { get; private set; } = string.Empty;

    /// <summary>Versão de destino (nova versão disponibilizada aos consumidores).</summary>
    public string ToVersion { get; private set; } = string.Empty;

    /// <summary>Data prevista para o anúncio oficial da nova versão aos consumidores.</summary>
    public DateTimeOffset AnnouncementDate { get; private set; }

    /// <summary>Data em que a nova versão estará disponível para uso pelos consumidores.</summary>
    public DateTimeOffset AvailabilityDate { get; private set; }

    /// <summary>Prazo limite para que os consumidores completem a migração para a nova versão.</summary>
    public DateTimeOffset MigrationDeadline { get; private set; }

    /// <summary>Data opcional de deprecação da versão anterior após a janela de migração.</summary>
    public DateTimeOffset? DeprecationDate { get; private set; }

    /// <summary>Status atual do plano de rollout no ciclo de vida.</summary>
    public VersionRolloutStatus Status { get; private set; } = VersionRolloutStatus.Draft;

    /// <summary>Observações adicionais sobre o plano de rollout (notas de release, instruções, etc.).</summary>
    public string? Notes { get; private set; }

    /// <summary>Data/hora UTC em que o plano de rollout foi criado.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Identificador do usuário que criou o plano de rollout.</summary>
    public string CreatedBy { get; private set; } = string.Empty;

    /// <summary>
    /// Cria um novo plano de rollout de versão com todas as datas-chave do ciclo de comunicação.
    /// O plano inicia no status Draft e deve ser explicitamente anunciado para notificar consumidores.
    /// </summary>
    public static VersionRolloutPlan Create(
        Guid tenantId,
        Guid apiAssetId,
        string fromVersion,
        string toVersion,
        DateTimeOffset announcementDate,
        DateTimeOffset availabilityDate,
        DateTimeOffset migrationDeadline,
        DateTimeOffset createdAt,
        string createdBy,
        DateTimeOffset? deprecationDate = null,
        string? notes = null)
    {
        Guard.Against.Default(tenantId);
        Guard.Against.Default(apiAssetId);
        Guard.Against.NullOrWhiteSpace(fromVersion);
        Guard.Against.NullOrWhiteSpace(toVersion);
        Guard.Against.NullOrWhiteSpace(createdBy);

        return new VersionRolloutPlan
        {
            Id = VersionRolloutPlanId.New(),
            TenantId = tenantId,
            ApiAssetId = apiAssetId,
            FromVersion = fromVersion,
            ToVersion = toVersion,
            AnnouncementDate = announcementDate,
            AvailabilityDate = availabilityDate,
            MigrationDeadline = migrationDeadline,
            DeprecationDate = deprecationDate,
            Status = VersionRolloutStatus.Draft,
            Notes = notes,
            CreatedAt = createdAt,
            CreatedBy = createdBy
        };
    }

    /// <summary>
    /// Anuncia o plano de rollout aos consumidores, tornando-o visível e ativo.
    /// Transição: Draft → Announced.
    /// Retorna falha se a transição de status for inválida.
    /// </summary>
    public Result<Unit> Announce()
        => TransitionTo(VersionRolloutStatus.Announced);

    /// <summary>
    /// Inicia o período de migração ativa dos consumidores para a nova versão.
    /// Transição: Announced → InProgress.
    /// Retorna falha se a transição de status for inválida.
    /// </summary>
    public Result<Unit> StartMigration()
        => TransitionTo(VersionRolloutStatus.InProgress);

    /// <summary>
    /// Marca o plano de rollout como concluído após migração dos consumidores.
    /// Transição: InProgress → Completed.
    /// Retorna falha se a transição de status for inválida.
    /// </summary>
    public Result<Unit> Complete()
        => TransitionTo(VersionRolloutStatus.Completed);

    /// <summary>
    /// Cancela o plano de rollout antes de sua conclusão.
    /// Transição: Draft/Announced/InProgress → Cancelled.
    /// Retorna falha se o plano já estiver em estado terminal.
    /// </summary>
    public Result<Unit> Cancel()
        => TransitionTo(VersionRolloutStatus.Cancelled);

    /// <summary>
    /// Transição centralizada de status — valida a transição e atualiza o estado.
    /// Elimina duplicação entre Announce/StartMigration/Complete/Cancel (DRY).
    /// </summary>
    private Result<Unit> TransitionTo(VersionRolloutStatus newStatus)
    {
        if (IsTerminal())
            return VersionCommunicationErrors.RolloutPlanAlreadyTerminal();

        if (!IsValidTransition(Status, newStatus))
            return VersionCommunicationErrors.InvalidRolloutStatusTransition(
                Status.ToString(), newStatus.ToString());

        Status = newStatus;
        return Unit.Value;
    }

    /// <summary>
    /// Verifica se o plano está em estado terminal (Completed ou Cancelled).
    /// Estados terminais não permitem novas transições.
    /// </summary>
    private bool IsTerminal()
        => Status is VersionRolloutStatus.Completed or VersionRolloutStatus.Cancelled;

    /// <summary>
    /// Valida se a transição de status é permitida segundo o ciclo de vida do rollout.
    /// Transições válidas: Draft → Announced → InProgress → Completed;
    /// Draft/Announced/InProgress → Cancelled.
    /// </summary>
    private static bool IsValidTransition(VersionRolloutStatus from, VersionRolloutStatus to) =>
        (from, to) switch
        {
            (VersionRolloutStatus.Draft, VersionRolloutStatus.Announced) => true,
            (VersionRolloutStatus.Announced, VersionRolloutStatus.InProgress) => true,
            (VersionRolloutStatus.InProgress, VersionRolloutStatus.Completed) => true,
            (VersionRolloutStatus.Draft, VersionRolloutStatus.Cancelled) => true,
            (VersionRolloutStatus.Announced, VersionRolloutStatus.Cancelled) => true,
            (VersionRolloutStatus.InProgress, VersionRolloutStatus.Cancelled) => true,
            _ => false
        };
}

/// <summary>Identificador fortemente tipado de VersionRolloutPlan.</summary>
public sealed record VersionRolloutPlanId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static VersionRolloutPlanId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static VersionRolloutPlanId From(Guid id) => new(id);
}
