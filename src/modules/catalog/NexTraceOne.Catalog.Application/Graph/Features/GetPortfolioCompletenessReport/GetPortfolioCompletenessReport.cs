using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Features.GetCatalogCompletenessScore;

namespace NexTraceOne.Catalog.Application.Graph.Features.GetPortfolioCompletenessReport;

/// <summary>
/// Feature: GetPortfolioCompletenessReport — relatório de completude do catálogo para o portfólio completo.
///
/// Lista todos os serviços do tenant com os respectivos scores de completude,
/// ordenados do mais incompleto para o mais completo.
/// Alimenta dashboards de plataforma e alertas de qualidade do catálogo.
/// </summary>
public static class GetPortfolioCompletenessReport
{
    public sealed record Query(string? DomainFilter, string? TeamFilter) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.DomainFilter).MaximumLength(200).When(x => x.DomainFilter is not null);
            RuleFor(x => x.TeamFilter).MaximumLength(200).When(x => x.TeamFilter is not null);
        }
    }

    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var services = await serviceAssetRepository.ListAllAsync(cancellationToken);

            var filtered = services
                .Where(s => request.DomainFilter is null
                    || s.Domain.Equals(request.DomainFilter, StringComparison.OrdinalIgnoreCase))
                .Where(s => request.TeamFilter is null
                    || s.TeamName.Equals(request.TeamFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var now = clock.UtcNow;
            var entries = filtered
                .Select(s => GetCatalogCompletenessScore.Handler.Compute(s, now))
                .OrderBy(r => r.TotalScore)
                .ToList();

            var avgScore = entries.Count > 0
                ? (int)Math.Round(entries.Average(e => e.TotalScore))
                : 0;

            var distribution = new Dictionary<string, int>
            {
                ["Excelente"] = entries.Count(e => e.MaturityLevel == "Excelente"),
                ["Maduro"] = entries.Count(e => e.MaturityLevel == "Maduro"),
                ["Em Desenvolvimento"] = entries.Count(e => e.MaturityLevel == "Em Desenvolvimento"),
                ["Nascente"] = entries.Count(e => e.MaturityLevel == "Nascente")
            };

            return new Response(
                TotalServices: entries.Count,
                AverageScore: avgScore,
                MaturityDistribution: distribution,
                Entries: entries);
        }
    }

    public sealed record Response(
        int TotalServices,
        int AverageScore,
        IReadOnlyDictionary<string, int> MaturityDistribution,
        IReadOnlyList<GetCatalogCompletenessScore.Response> Entries);
}
