using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.StartPostIncidentReview;

/// <summary>
/// Feature: StartPostIncidentReview — inicia um processo formal de Post-Incident Review (PIR)
/// para um incidente existente. Valida que o incidente existe e que não há PIR duplicado.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class StartPostIncidentReview
{
    /// <summary>Comando para iniciar um PIR.</summary>
    public sealed record Command(
        Guid IncidentId,
        string ResponsibleTeam,
        string? Facilitator) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando de início de PIR.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty();
            RuleFor(x => x.ResponsibleTeam).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Facilitator).MaximumLength(200).When(x => x.Facilitator is not null);
        }
    }

    /// <summary>
    /// Handler que cria um novo PIR na fase inicial de recolha de factos.
    /// Valida que o incidente existe e que não há PIR duplicado.
    /// </summary>
    public sealed class Handler(
        IIncidentStore incidentStore,
        IPostIncidentReviewRepository reviewRepository,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Validar que o incidente existe (IIncidentStore usa método síncrono)
            if (!incidentStore.IncidentExists(request.IncidentId.ToString()))
            {
                return IncidentErrors.IncidentNotFound(request.IncidentId.ToString());
            }

            // Validar que não existe PIR duplicado
            var existing = await reviewRepository.GetByIncidentIdAsync(request.IncidentId, cancellationToken);
            if (existing is not null)
            {
                return IncidentErrors.PirAlreadyExists(request.IncidentId.ToString());
            }

            var now = dateTimeProvider.UtcNow;
            var review = PostIncidentReview.Start(
                PostIncidentReviewId.New(),
                request.IncidentId,
                request.ResponsibleTeam,
                request.Facilitator,
                now);

            await reviewRepository.AddAsync(review, cancellationToken);

            return new Response(
                review.Id.Value,
                review.IncidentId,
                review.CurrentPhase.ToString(),
                review.Outcome.ToString(),
                review.ResponsibleTeam,
                review.Facilitator,
                review.StartedAt);
        }
    }

    /// <summary>Resposta da criação do PIR.</summary>
    public sealed record Response(
        Guid ReviewId,
        Guid IncidentId,
        string CurrentPhase,
        string Outcome,
        string ResponsibleTeam,
        string? Facilitator,
        DateTimeOffset StartedAt);
}
