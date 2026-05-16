using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;

namespace NexTraceOne.Catalog.Application.Graph.Features.GetServiceMaturityBenchmark;

/// <summary>
/// Feature: GetServiceMaturityBenchmark — compara maturidade entre equipas e domínios.
/// Produz um relatório de benchmark mostrando a posição relativa de cada equipa e domínio
/// com base na qualidade dos atributos de ownership, repositório, documentação e descrição dos serviços.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class GetServiceMaturityBenchmark
{
    /// <summary>Query para obter o benchmark de maturidade por equipa e domínio.</summary>
    public sealed record Query(
        string? Domain = null,
        string? TeamName = null,
        int TopN = 10) : IQuery<Response>;

    /// <summary>Valida a entrada da query de benchmark de maturidade.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TopN).InclusiveBetween(1, 100);
        }
    }

    /// <summary>
    /// Handler que agrega pontuações de maturidade por equipa e domínio.
    /// O MaturityScore por serviço é calculado com base em quatro atributos:
    /// ownership (TeamName + TechnicalOwner), repositório, documentação e descrição.
    /// Cada atributo contribui com 0.25 do score total (0.0–1.0).
    /// Classifica equipas e domínios por score decrescente e aplica o limite TopN.
    /// </summary>
    public sealed class Handler(IServiceAssetRepository serviceAssetRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var (services, _) = await serviceAssetRepository.ListFilteredAsync(
                teamName: request.TeamName,
                domain: request.Domain,
                serviceType: null,
                criticality: null,
                lifecycleStatus: null,
                exposureType: null,
                searchTerm: null,
                page: 1,
                pageSize: 10_000,
                cancellationToken);

            // Agregar por equipa
            var teamGroups = services
                .Where(s => !string.IsNullOrWhiteSpace(s.TeamName))
                .GroupBy(s => s.TeamName);

            var teamEntries = teamGroups
                .Select(g =>
                {
                    var avgScore = g.Average(s => ComputeServiceScore(s));
                    return (TeamName: g.Key, Count: g.Count(), Score: Math.Round(avgScore, 4));
                })
                .OrderByDescending(t => t.Score)
                .Take(request.TopN)
                .Select((t, i) => new TeamBenchmarkEntry(t.TeamName, t.Count, t.Score, i + 1))
                .ToList()
                .AsReadOnly();

            // Agregar por domínio
            var domainGroups = services
                .Where(s => !string.IsNullOrWhiteSpace(s.Domain))
                .GroupBy(s => s.Domain);

            var domainEntries = domainGroups
                .Select(g =>
                {
                    var avgScore = g.Average(s => ComputeServiceScore(s));
                    return (DomainName: g.Key, Count: g.Count(), Score: Math.Round(avgScore, 4));
                })
                .OrderByDescending(d => d.Score)
                .Take(request.TopN)
                .Select((d, i) => new DomainBenchmarkEntry(d.DomainName, d.Count, d.Score, i + 1))
                .ToList()
                .AsReadOnly();

            return new Response(
                Teams: teamEntries,
                Domains: domainEntries,
                BenchmarkComputedAt: DateTimeOffset.UtcNow.ToString("O"));
        }

        /// <summary>
        /// Calcula o score de maturidade simplificado de um serviço (0.0 a 1.0).
        /// Pontuação: ownership completo +0.25, repositório +0.25, documentação +0.25, descrição +0.25.
        /// </summary>
        private static double ComputeServiceScore(NexTraceOne.Catalog.Domain.Graph.Entities.ServiceAsset service)
        {
            double score = 0;

            if (!string.IsNullOrWhiteSpace(service.TeamName) && !string.IsNullOrWhiteSpace(service.TechnicalOwner))
                score += 0.25;

            if (!string.IsNullOrWhiteSpace(service.RepositoryUrl))
                score += 0.25;

            if (!string.IsNullOrWhiteSpace(service.DocumentationUrl))
                score += 0.25;

            if (!string.IsNullOrWhiteSpace(service.Description))
                score += 0.25;

            return score;
        }
    }

    /// <summary>Entrada de benchmark de maturidade por equipa.</summary>
    public sealed record TeamBenchmarkEntry(
        string TeamName,
        int ServiceCount,
        double AverageMaturityScore,
        int Rank);

    /// <summary>Entrada de benchmark de maturidade por domínio.</summary>
    public sealed record DomainBenchmarkEntry(
        string DomainName,
        int ServiceCount,
        double AverageMaturityScore,
        int Rank);

    /// <summary>Resposta do benchmark de maturidade por equipa e domínio.</summary>
    public sealed record Response(
        IReadOnlyList<TeamBenchmarkEntry> Teams,
        IReadOnlyList<DomainBenchmarkEntry> Domains,
        string BenchmarkComputedAt);
}
