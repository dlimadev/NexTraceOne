using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;
using NexTraceOne.Catalog.Domain.DeveloperExperience.Entities;

namespace NexTraceOne.Catalog.Application.DeveloperExperience.Features.SubmitDeveloperSurvey;

/// <summary>
/// Feature: SubmitDeveloperSurvey — regista a resposta de um desenvolvedor ao survey de NPS e satisfação.
/// </summary>
public static class SubmitDeveloperSurvey
{
    public sealed record Command(
        string TeamId,
        string TeamName,
        string? ServiceId,
        string RespondentId,
        string Period,
        int NpsScore,
        decimal ToolSatisfaction,
        decimal ProcessSatisfaction,
        decimal PlatformSatisfaction,
        string? Comments) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TeamId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.TeamName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.RespondentId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Period)
                .Must(x => new[] { "weekly", "monthly", "quarterly" }.Contains(x))
                .WithMessage("Valid periods: weekly, monthly, quarterly.");
            RuleFor(x => x.NpsScore).InclusiveBetween(0, 10);
            RuleFor(x => x.ToolSatisfaction).InclusiveBetween(0m, 5m);
            RuleFor(x => x.ProcessSatisfaction).InclusiveBetween(0m, 5m);
            RuleFor(x => x.PlatformSatisfaction).InclusiveBetween(0m, 5m);
            RuleFor(x => x.Comments).MaximumLength(2000).When(x => x.Comments is not null);
        }
    }

    public sealed class Handler(
        IDeveloperSurveyRepository repository,
        IDeveloperExperienceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var result = DeveloperSurvey.Create(
                request.TeamId,
                request.TeamName,
                request.ServiceId,
                request.RespondentId,
                request.Period,
                request.NpsScore,
                request.ToolSatisfaction,
                request.ProcessSatisfaction,
                request.PlatformSatisfaction,
                request.Comments,
                clock.UtcNow);

            if (!result.IsSuccess) return result.Error;

            await repository.AddAsync(result.Value!, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var survey = result.Value!;
            return Result<Response>.Success(new Response(
                survey.Id.Value,
                survey.TeamId,
                survey.RespondentId,
                survey.NpsScore,
                survey.NpsCategory,
                survey.SubmittedAt));
        }
    }

    public sealed record Response(
        Guid SurveyId,
        string TeamId,
        string RespondentId,
        int NpsScore,
        string NpsCategory,
        DateTimeOffset SubmittedAt);
}
