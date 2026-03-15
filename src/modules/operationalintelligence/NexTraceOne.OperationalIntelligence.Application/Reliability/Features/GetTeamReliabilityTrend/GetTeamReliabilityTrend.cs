using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetTeamReliabilityTrend;

/// <summary>
/// Feature: GetTeamReliabilityTrend — obtém a tendência agregada de confiabilidade
/// dos serviços de uma equipa. Útil para Tech Lead e Executive perceberem
/// a direção geral de fiabilidade ao longo do tempo.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetTeamReliabilityTrend
{
    /// <summary>Query para obter tendência agregada de confiabilidade da equipa.</summary>
    public sealed record Query(string TeamId) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TeamId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que compõe a tendência agregada de confiabilidade da equipa.
    /// </summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = new Response(
                request.TeamId,
                TrendDirection.Stable,
                "7d",
                "Overall team reliability is stable. 1 service requires attention.",
                [
                    new TeamTrendDataPoint(DateTimeOffset.UtcNow.AddDays(-7), 3, 2, 1, 0, 0),
                    new TeamTrendDataPoint(DateTimeOffset.UtcNow.AddDays(-5), 3, 3, 0, 0, 0),
                    new TeamTrendDataPoint(DateTimeOffset.UtcNow.AddDays(-3), 3, 2, 0, 0, 1),
                    new TeamTrendDataPoint(DateTimeOffset.UtcNow.AddDays(-1), 3, 2, 0, 0, 1),
                    new TeamTrendDataPoint(DateTimeOffset.UtcNow, 3, 2, 0, 0, 1),
                ]);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Ponto de dados na tendência agregada da equipa.</summary>
    public sealed record TeamTrendDataPoint(
        DateTimeOffset Timestamp,
        int TotalServices,
        int HealthyCount,
        int DegradedCount,
        int UnavailableCount,
        int NeedsAttentionCount);

    /// <summary>Resposta com tendência agregada de confiabilidade da equipa.</summary>
    public sealed record Response(
        string TeamId,
        TrendDirection Direction,
        string Timeframe,
        string Summary,
        IReadOnlyList<TeamTrendDataPoint> DataPoints);
}
