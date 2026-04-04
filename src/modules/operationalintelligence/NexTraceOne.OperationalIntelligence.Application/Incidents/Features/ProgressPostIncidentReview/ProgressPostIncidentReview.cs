using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ProgressPostIncidentReview;

/// <summary>
/// Feature: ProgressPostIncidentReview — avança um PIR existente para a próxima fase,
/// atualizando dados de análise (causa raiz, ações preventivas, timeline, resumo).
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ProgressPostIncidentReview
{
    /// <summary>Comando para avançar o PIR para a próxima fase.</summary>
    public sealed record Command(
        Guid ReviewId,
        PostIncidentReviewPhase NewPhase,
        PostIncidentReviewOutcome? Outcome,
        string? RootCauseAnalysis,
        string? PreventiveActionsJson,
        string? TimelineNarrative,
        string? Summary) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando de progressão do PIR.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReviewId).NotEmpty();
            RuleFor(x => x.NewPhase).IsInEnum();
            RuleFor(x => x.RootCauseAnalysis).MaximumLength(5000).When(x => x.RootCauseAnalysis is not null);
            RuleFor(x => x.Summary).MaximumLength(5000).When(x => x.Summary is not null);
            RuleFor(x => x.TimelineNarrative).MaximumLength(10000).When(x => x.TimelineNarrative is not null);
        }
    }

    /// <summary>
    /// Handler que avança o PIR para a fase indicada com os dados fornecidos.
    /// </summary>
    public sealed class Handler(
        IPostIncidentReviewRepository reviewRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var review = await reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
            if (review is null)
            {
                return IncidentErrors.PirNotFound(request.ReviewId.ToString());
            }

            if (review.IsCompleted)
            {
                return IncidentErrors.PirAlreadyCompleted(request.ReviewId.ToString());
            }

            review.Progress(
                request.NewPhase,
                request.Outcome,
                request.RootCauseAnalysis,
                request.PreventiveActionsJson,
                request.TimelineNarrative,
                request.Summary,
                completedAt: null);

            await reviewRepository.UpdateAsync(review, cancellationToken);

            return new Response(
                review.Id.Value,
                review.IncidentId,
                review.CurrentPhase.ToString(),
                review.Outcome.ToString(),
                review.IsCompleted,
                review.RootCauseAnalysis,
                review.Summary,
                review.CompletedAt);
        }
    }

    /// <summary>Resposta da progressão do PIR.</summary>
    public sealed record Response(
        Guid ReviewId,
        Guid IncidentId,
        string CurrentPhase,
        string Outcome,
        bool IsCompleted,
        string? RootCauseAnalysis,
        string? Summary,
        DateTimeOffset? CompletedAt);
}
