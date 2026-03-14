using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.DeveloperPortal.Application.Features.GetMyApis;

/// <summary>
/// Feature: GetMyApis — lista APIs de que o utilizador é owner ou responsável.
/// Perspetiva do produtor: permite ver quem consome, métricas e alertas.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetMyApis
{
    /// <summary>Query para listar APIs de um owner.</summary>
    public sealed record Query(Guid OwnerId, int Page = 1, int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida os parâmetros da consulta de APIs do owner.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.OwnerId).NotEmpty();
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>
    /// Handler que retorna APIs de um owner.
    /// Em produção, consulta EngineeringGraph para APIs com ownership atribuído.
    /// </summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var result = new Response(
                Items: new List<OwnedApiDto>().AsReadOnly(),
                TotalCount: 0);

            return Task.FromResult(Result<Response>.Success(result));
        }
    }

    /// <summary>DTO de API de propriedade do utilizador com métricas de consumo.</summary>
    public sealed record OwnedApiDto(
        Guid ApiAssetId,
        string Name,
        string? Description,
        string? CurrentVersion,
        string Status,
        int ConsumerCount,
        int SubscriberCount,
        DateTimeOffset? LastDeployment);

    /// <summary>Resposta com APIs de propriedade do owner.</summary>
    public sealed record Response(IReadOnlyList<OwnedApiDto> Items, int TotalCount);
}
