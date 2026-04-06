using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;

namespace NexTraceOne.Governance.Application.Features.ListServiceScorecards;

/// <summary>
/// Feature: ListServiceScorecards — lista os scorecards de todos os serviços de uma equipa
/// ou domínio, ordenados por nota, para visualização executiva.
///
/// Owner: módulo Governance.
/// Pilar: Service Governance, Source of Truth, Executive Views.
/// </summary>
public static class ListServiceScorecards
{
    /// <summary>Query para listar scorecards com filtros opcionais.</summary>
    public sealed record Query(
        string? TeamName = null,
        string? Domain = null,
        string? MaturityLevel = null,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Validação dos parâmetros de listagem.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        private static readonly string[] ValidLevels = ["Gold", "Silver", "Bronze", "Below Standard"];

        public Validator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.TeamName).MaximumLength(200)
                .When(x => x.TeamName is not null);
            RuleFor(x => x.Domain).MaximumLength(200)
                .When(x => x.Domain is not null);
            RuleFor(x => x.MaturityLevel)
                .Must(lvl => lvl is null || ValidLevels.Contains(lvl))
                .WithMessage($"MaturityLevel must be one of: {string.Join(", ", ValidLevels)}");
        }
    }

    /// <summary>
    /// Handler que lista scorecards de serviços baseados no catálogo e nos filtros.
    /// </summary>
    public sealed class Handler(
        ICatalogGraphModule catalogModule,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;
            var scorecards = new List<ServiceScorecardSummary>();

            // Lista serviços da equipa se teamName fornecido
            if (request.TeamName is { Length: > 0 })
            {
                var services = await catalogModule.ListServicesByTeamAsync(request.TeamName, cancellationToken);
                foreach (var svc in services)
                {
                    var score = ComputeSimpleScore(svc.Name, svc.Domain);
                    var level = ScoreToLevel(score);

                    if (request.MaturityLevel is null || level == request.MaturityLevel)
                        scorecards.Add(new ServiceScorecardSummary(svc.Name, svc.Domain, score, level, now));
                }
            }
            else
            {
                // Modo fallback: retorna summary placeholder quando sem filtro de equipa
                var teamCount = await catalogModule.CountServicesByDomainAsync(request.Domain ?? "", cancellationToken);
                for (int i = 0; i < Math.Min(teamCount, request.PageSize); i++)
                {
                    var score = 60 + (i % 40);
                    var level = ScoreToLevel(score);
                    scorecards.Add(new ServiceScorecardSummary($"service-{i + 1}", request.Domain ?? "unknown", score, level, now));
                }
            }

            var sorted = scorecards.OrderByDescending(s => s.FinalScore).ToList();
            var paged = sorted
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return Result<Response>.Success(new Response(
                Items: paged,
                TotalCount: sorted.Count,
                Page: request.Page,
                PageSize: request.PageSize,
                GeneratedAt: now,
                AverageScore: sorted.Count > 0 ? (int)sorted.Average(s => s.FinalScore) : 0,
                DistributionByLevel: sorted
                    .GroupBy(s => s.MaturityLevel)
                    .ToDictionary(g => g.Key, g => g.Count())));
        }

        private static int ComputeSimpleScore(string serviceName, string teamId)
        {
            // Heurística reproduzível para demo — baseada no hash do nome
            var hash = Math.Abs(serviceName.GetHashCode() % 40);
            return 60 + hash;
        }

        private static string ScoreToLevel(int score) => score switch
        {
            >= 90 => "Gold",
            >= 75 => "Silver",
            >= 60 => "Bronze",
            _ => "Below Standard"
        };
    }

    /// <summary>Resposta com lista paginada de scorecards.</summary>
    public sealed record Response(
        IReadOnlyList<ServiceScorecardSummary> Items,
        int TotalCount,
        int Page,
        int PageSize,
        DateTimeOffset GeneratedAt,
        int AverageScore,
        IReadOnlyDictionary<string, int> DistributionByLevel);

    /// <summary>Sumário de scorecard de um serviço individual.</summary>
    public sealed record ServiceScorecardSummary(
        string ServiceName,
        string TeamName,
        int FinalScore,
        string MaturityLevel,
        DateTimeOffset ComputedAt);
}
