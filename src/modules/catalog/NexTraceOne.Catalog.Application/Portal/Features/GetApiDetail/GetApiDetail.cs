using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Domain.Portal.Errors;

namespace NexTraceOne.Catalog.Application.Portal.Features.GetApiDetail;

/// <summary>
/// Feature: GetApiDetail — retorna detalhes completos de uma API incluindo sinais de confiança.
/// Inclui: owner, status, versão, frescor, playground habilitado, score de completude.
/// </summary>
public static class GetApiDetail
{
    /// <summary>Query para obter detalhes de uma API por identificador.</summary>
    public sealed record Query(Guid ApiAssetId) : IQuery<Response>;

    /// <summary>Valida os parâmetros da consulta de detalhes de API.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que retorna detalhes enriquecidos de uma API.
    /// Em produção, agrega dados do Catalog Graph, Contracts e ChangeIntelligence.
    /// </summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // MVP1: retorna erro de não encontrado — em produção consulta Catalog Graph.
            return Task.FromResult<Result<Response>>(
                DeveloperPortalErrors.ApiNotFound(request.ApiAssetId.ToString()));
        }
    }

    /// <summary>Sinais de confiança e qualidade da API.</summary>
    public sealed record TrustSignals(
        string? Owner,
        string? Team,
        string Status,
        DateTimeOffset? LastUpdated,
        string? ContractVersion,
        bool IsContractValid,
        bool PlaygroundEnabled,
        bool IsDeprecated,
        DateTimeOffset? DeprecationDate,
        string? RecommendedVersion,
        decimal DocumentationCompleteness,
        decimal OverallTrustScore);

    /// <summary>Resposta com detalhes completos da API incluindo sinais de confiança.</summary>
    public sealed record Response(
        Guid ApiAssetId,
        string Name,
        string? Description,
        string? RoutePattern,
        string? Owner,
        string? Team,
        string Status,
        string? CurrentVersion,
        string? Environment,
        TrustSignals Trust,
        int ConsumerCount,
        int SubscriberCount,
        DateTimeOffset? LastDeployment,
        IReadOnlyList<string> Tags);
}
