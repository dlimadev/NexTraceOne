using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.DeveloperPortal.Application.Features.GetApiHealth;

/// <summary>
/// Feature: GetApiHealth — retorna indicadores de saúde e disponibilidade de uma API.
/// Inclui SLO, latência, error rate e status do último deployment.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetApiHealth
{
    /// <summary>Query para obter indicadores de saúde de uma API.</summary>
    public sealed record Query(Guid ApiAssetId) : IQuery<Response>;

    /// <summary>Valida os parâmetros da consulta de saúde.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que retorna indicadores de saúde da API.
    /// MVP1: retorna dados estáticos — em produção, consulta RuntimeIntelligence.
    /// </summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var result = new Response(
                ApiAssetId: request.ApiAssetId,
                HealthStatus: "Unknown",
                SloCompliance: null,
                AverageLatencyMs: null,
                ErrorRate: null,
                LastDeploymentStatus: null,
                LastCheckedAt: null);

            return Task.FromResult(Result<Response>.Success(result));
        }
    }

    /// <summary>Resposta com indicadores de saúde da API.</summary>
    public sealed record Response(
        Guid ApiAssetId,
        string HealthStatus,
        decimal? SloCompliance,
        long? AverageLatencyMs,
        decimal? ErrorRate,
        string? LastDeploymentStatus,
        DateTimeOffset? LastCheckedAt);
}
