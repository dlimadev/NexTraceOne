using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.ApproveHealingRecommendation;

/// <summary>
/// Feature: ApproveHealingRecommendation — aprova uma recomendação de self-healing,
/// transicionando-a de Proposed para Approved.
/// Requer identificação do utilizador aprovador para trilha de auditoria.
///
/// Pilar: Operational Reliability + Self-Healing Recommendations.
/// Ideia 7 — Self-Healing Recommendations.
/// </summary>
public static class ApproveHealingRecommendation
{
    /// <summary>Comando para aprovar uma recomendação de self-healing.</summary>
    public sealed record Command(Guid RecommendationId) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando de aprovação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RecommendationId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que aprova a recomendação, registando o utilizador e timestamp.
    /// </summary>
    public sealed class Handler(
        IHealingRecommendationRepository repository,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var recommendation = await repository.GetByIdAsync(
                HealingRecommendationId.From(request.RecommendationId), cancellationToken);

            if (recommendation is null)
                return ReliabilityErrors.HealingRecommendationNotFound(request.RecommendationId.ToString());

            var now = dateTimeProvider.UtcNow;
            var result = recommendation.Approve(currentUser.Id, now);

            if (result.IsFailure)
                return result.Error;

            repository.Update(recommendation);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                recommendation.Id.Value,
                recommendation.Status.ToString(),
                recommendation.ApprovedByUserId!,
                recommendation.ApprovedAt!.Value));
        }
    }

    /// <summary>Resposta com os dados da aprovação.</summary>
    public sealed record Response(
        Guid RecommendationId,
        string Status,
        string ApprovedByUserId,
        DateTimeOffset ApprovedAt);
}
