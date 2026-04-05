using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;

namespace NexTraceOne.Catalog.Application.DeveloperExperience.Features.GetDeveloperNpsSummary;

/// <summary>
/// Feature: GetDeveloperNpsSummary — calcula e retorna o resumo de NPS agregado para uma equipa e período.
/// </summary>
public static class GetDeveloperNpsSummary
{
    public sealed record Query(
        string TeamId,
        string? Period,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TeamId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Period).MaximumLength(50).When(x => x.Period is not null);
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    public sealed class Handler(IDeveloperSurveyRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var surveys = await repository.ListByTeamAsync(
                request.TeamId,
                request.Period,
                request.Page,
                request.PageSize,
                cancellationToken);

            if (surveys.Count == 0)
            {
                return Result<Response>.Success(new Response(
                    request.TeamId,
                    request.Period,
                    TotalResponses: 0,
                    NpsScore: 0m,
                    PromoterPercent: 0m,
                    PassivePercent: 0m,
                    DetractorPercent: 0m,
                    PromoterCount: 0,
                    PassiveCount: 0,
                    DetractorCount: 0,
                    AvgToolSatisfaction: 0m,
                    AvgProcessSatisfaction: 0m,
                    AvgPlatformSatisfaction: 0m));
            }

            var total = surveys.Count;
            var promoterCount = surveys.Count(s => s.NpsCategory == "Promoter");
            var passiveCount = surveys.Count(s => s.NpsCategory == "Passive");
            var detractorCount = surveys.Count(s => s.NpsCategory == "Detractor");

            var npsScore = Math.Round(((decimal)(promoterCount - detractorCount) * 100m) / total, 1);
            var promoterPercent = Math.Round((decimal)promoterCount * 100m / total, 1);
            var passivePercent = Math.Round((decimal)passiveCount * 100m / total, 1);
            var detractorPercent = Math.Round((decimal)detractorCount * 100m / total, 1);

            var avgTool = Math.Round(surveys.Average(s => s.ToolSatisfaction), 2);
            var avgProcess = Math.Round(surveys.Average(s => s.ProcessSatisfaction), 2);
            var avgPlatform = Math.Round(surveys.Average(s => s.PlatformSatisfaction), 2);

            return Result<Response>.Success(new Response(
                request.TeamId,
                request.Period,
                total,
                npsScore,
                promoterPercent,
                passivePercent,
                detractorPercent,
                promoterCount,
                passiveCount,
                detractorCount,
                avgTool,
                avgProcess,
                avgPlatform));
        }
    }

    public sealed record Response(
        string TeamId,
        string? Period,
        int TotalResponses,
        decimal NpsScore,
        decimal PromoterPercent,
        decimal PassivePercent,
        decimal DetractorPercent,
        int PromoterCount,
        int PassiveCount,
        int DetractorCount,
        decimal AvgToolSatisfaction,
        decimal AvgProcessSatisfaction,
        decimal AvgPlatformSatisfaction);
}
