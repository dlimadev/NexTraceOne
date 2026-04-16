using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetRiskScoreTrend;

/// <summary>
/// Feature: GetRiskScoreTrend — retorna a série temporal de scores de risco de um serviço.
///
/// Responde à pergunta: "como evoluiu o risco de mudança deste serviço nas últimas N releases?"
/// Os dados alimentam o gráfico de tendência de risco no frontend (Gap 12).
///
/// Algoritmo:
///   1. Lista as N releases mais recentes do serviço (ordenadas por data descendente).
///   2. Para cada release, tenta obter o score de mudança computado.
///   3. Retorna a série temporal com releaseId, versão, ambiente, data e score.
///
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetRiskScoreTrend
{
    private const int DefaultLimit = 30;
    private const int MaxLimit = 100;

    /// <summary>Query para obter a tendência de risco de um serviço.</summary>
    public sealed record Query(
        string ServiceName,
        string? Environment,
        int? Limit) : IQuery<Response>;

    /// <summary>Valida a entrada da query de tendência de risco.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName)
                .NotEmpty()
                .MaximumLength(500);

            RuleFor(x => x.Limit)
                .InclusiveBetween(1, MaxLimit)
                .When(x => x.Limit.HasValue);
        }
    }

    /// <summary>Handler que compõe a série temporal de scores de risco por serviço.</summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IChangeScoreRepository scoreRepository,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var limit = request.Limit ?? DefaultLimit;

            var releases = await releaseRepository.ListByServiceNameAsync(
                request.ServiceName,
                page: 1,
                pageSize: limit,
                cancellationToken: cancellationToken);

            var dataPoints = new List<RiskScoreDataPoint>(releases.Count);

            foreach (var release in releases)
            {
                if (request.Environment is not null
                    && !release.Environment.Equals(request.Environment, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var score = await scoreRepository.GetByReleaseIdAsync(release.Id, cancellationToken);

                dataPoints.Add(new RiskScoreDataPoint(
                    ReleaseId: release.Id.Value,
                    Version: release.Version,
                    Environment: release.Environment,
                    ChangeLevel: release.ChangeLevel.ToString(),
                    Status: release.Status.ToString(),
                    Score: score?.Score,
                    CreatedAt: release.CreatedAt));
            }

            // Ordenar cronologicamente (mais antigo → mais recente) para o gráfico
            dataPoints.Sort((a, b) => a.CreatedAt.CompareTo(b.CreatedAt));

            return new Response(
                ServiceName: request.ServiceName,
                Environment: request.Environment,
                DataPoints: dataPoints,
                GeneratedAt: dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Ponto de dado na série temporal de risco.</summary>
    public sealed record RiskScoreDataPoint(
        Guid ReleaseId,
        string Version,
        string Environment,
        string ChangeLevel,
        string Status,
        decimal? Score,
        DateTimeOffset CreatedAt);

    /// <summary>Resposta com a série temporal de scores de risco de um serviço.</summary>
    public sealed record Response(
        string ServiceName,
        string? Environment,
        IReadOnlyList<RiskScoreDataPoint> DataPoints,
        DateTimeOffset GeneratedAt);
}
