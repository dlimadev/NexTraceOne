using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Enums;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Errors;

namespace NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;

/// <summary>
/// Aggregate Root que representa uma solicitação de promoção de release entre ambientes.
/// Gerencia o ciclo de vida da promoção: Pending → InEvaluation → Approved/Rejected/Blocked/Cancelled.
/// </summary>
public sealed class PromotionRequest : AggregateRoot<PromotionRequestId>
{
    private PromotionRequest() { }

    /// <summary>Identificador da release que será promovida.</summary>
    public Guid ReleaseId { get; private set; }

    /// <summary>Identificador do ambiente de origem da promoção.</summary>
    public DeploymentEnvironmentId SourceEnvironmentId { get; private set; } = default!;

    /// <summary>Identificador do ambiente de destino da promoção.</summary>
    public DeploymentEnvironmentId TargetEnvironmentId { get; private set; } = default!;

    /// <summary>Identificador do usuário que solicitou a promoção.</summary>
    public string RequestedBy { get; private set; } = string.Empty;

    /// <summary>Status atual da solicitação de promoção.</summary>
    public PromotionStatus Status { get; private set; } = PromotionStatus.Pending;

    /// <summary>Justificativa opcional para a solicitação de promoção.</summary>
    public string? Justification { get; private set; }

    /// <summary>Data/hora UTC em que a promoção foi solicitada.</summary>
    public DateTimeOffset RequestedAt { get; private set; }

    /// <summary>Data/hora UTC em que a promoção foi concluída (aprovada, rejeitada, bloqueada ou cancelada).</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// Cria uma nova solicitação de promoção de release entre ambientes.
    /// </summary>
    public static PromotionRequest Create(
        Guid releaseId,
        DeploymentEnvironmentId sourceEnvironmentId,
        DeploymentEnvironmentId targetEnvironmentId,
        string requestedBy,
        DateTimeOffset requestedAt)
    {
        Guard.Against.Default(releaseId);
        Guard.Against.Null(sourceEnvironmentId);
        Guard.Against.Null(targetEnvironmentId);
        Guard.Against.NullOrWhiteSpace(requestedBy);
        Guard.Against.StringTooLong(requestedBy, 500);

        return new PromotionRequest
        {
            Id = PromotionRequestId.New(),
            ReleaseId = releaseId,
            SourceEnvironmentId = sourceEnvironmentId,
            TargetEnvironmentId = targetEnvironmentId,
            RequestedBy = requestedBy,
            Status = PromotionStatus.Pending,
            RequestedAt = requestedAt
        };
    }

    /// <summary>
    /// Aprova a solicitação de promoção.
    /// Retorna falha se a transição de status for inválida.
    /// </summary>
    public Result<Unit> Approve(DateTimeOffset completedAt)
        => TransitionTo(PromotionStatus.Approved, completedAt);

    /// <summary>
    /// Rejeita a solicitação de promoção.
    /// Retorna falha se a transição de status for inválida.
    /// </summary>
    public Result<Unit> Reject(DateTimeOffset completedAt)
        => TransitionTo(PromotionStatus.Rejected, completedAt);

    /// <summary>
    /// Bloqueia a solicitação de promoção por regra de governança.
    /// Retorna falha se a transição de status for inválida.
    /// </summary>
    public Result<Unit> Block(DateTimeOffset completedAt)
        => TransitionTo(PromotionStatus.Blocked, completedAt);

    /// <summary>
    /// Cancela a solicitação de promoção antes de sua conclusão.
    /// Retorna falha se a transição de status for inválida.
    /// </summary>
    public Result<Unit> Cancel(DateTimeOffset completedAt)
        => TransitionTo(PromotionStatus.Cancelled, completedAt);

    /// <summary>
    /// Inicia o processo de avaliação da solicitação de promoção.
    /// Transição: Pending → InEvaluation.
    /// Retorna falha se a transição de status for inválida.
    /// </summary>
    public Result<Unit> StartEvaluation()
        => TransitionTo(PromotionStatus.InEvaluation);

    /// <summary>
    /// Define a justificativa para a solicitação de promoção.
    /// </summary>
    public void SetJustification(string text)
    {
        Guard.Against.StringTooLong(text, 4000);
        Justification = text;
    }

    /// <summary>
    /// Transição centralizada de status — valida a transição, atualiza o estado
    /// e marca a conclusão quando aplicável.
    /// Elimina duplicação entre Approve/Reject/Block/Cancel/StartEvaluation (DRY).
    /// </summary>
    private Result<Unit> TransitionTo(PromotionStatus newStatus, DateTimeOffset? completedAt = null)
    {
        var result = ValidateTransition(newStatus);
        if (result.IsFailure)
            return result;

        Status = newStatus;
        CompletedAt = completedAt;
        return Unit.Value;
    }

    /// <summary>
    /// Valida se a transição de status é permitida.
    /// Transições válidas: Pending → InEvaluation → (Approved|Rejected|Blocked);
    /// Pending/InEvaluation → Cancelled.
    /// </summary>
    private Result<Unit> ValidateTransition(PromotionStatus newStatus)
    {
        if (CompletedAt is not null)
            return PromotionErrors.AlreadyCompleted();

        var isValid = (Status, newStatus) switch
        {
            (PromotionStatus.Pending, PromotionStatus.InEvaluation) => true,
            (PromotionStatus.Pending, PromotionStatus.Cancelled) => true,
            (PromotionStatus.InEvaluation, PromotionStatus.Approved) => true,
            (PromotionStatus.InEvaluation, PromotionStatus.Rejected) => true,
            (PromotionStatus.InEvaluation, PromotionStatus.Blocked) => true,
            (PromotionStatus.InEvaluation, PromotionStatus.Cancelled) => true,
            _ => false
        };

        if (!isValid)
            return PromotionErrors.InvalidStatusTransition(Status.ToString(), newStatus.ToString());

        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de PromotionRequest.</summary>
public sealed record PromotionRequestId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static PromotionRequestId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static PromotionRequestId From(Guid id) => new(id);
}
