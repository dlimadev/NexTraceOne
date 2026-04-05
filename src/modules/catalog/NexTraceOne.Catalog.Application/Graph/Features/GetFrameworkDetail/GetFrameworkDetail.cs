using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.GetFrameworkDetail;

/// <summary>
/// Feature: GetFrameworkDetail — obtém os detalhes de Framework/SDK de um serviço.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetFrameworkDetail
{
    /// <summary>Query de detalhe de framework pelo identificador do serviço.</summary>
    public sealed record Query(Guid ServiceAssetId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de detalhe de framework.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceAssetId).NotEmpty();
        }
    }

    /// <summary>Handler que retorna o detalhe de framework de um serviço.</summary>
    public sealed class Handler(
        IFrameworkAssetDetailRepository frameworkRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var detail = await frameworkRepository.GetByServiceAssetIdAsync(
                ServiceAssetId.From(request.ServiceAssetId), cancellationToken);

            if (detail is null)
                return CatalogGraphErrors.FrameworkDetailNotFound(request.ServiceAssetId);

            return new Response(
                detail.Id.Value,
                detail.ServiceAssetId.Value,
                detail.PackageName,
                detail.Language,
                detail.PackageManager,
                detail.ArtifactRegistryUrl,
                detail.LatestVersion,
                detail.MinSupportedVersion,
                detail.TargetPlatform,
                detail.LicenseType,
                detail.BuildPipelineUrl,
                detail.ChangelogUrl,
                detail.KnownConsumerCount);
        }
    }

    /// <summary>Resposta do detalhe de framework de um serviço.</summary>
    public sealed record Response(
        Guid FrameworkAssetDetailId,
        Guid ServiceAssetId,
        string PackageName,
        string Language,
        string PackageManager,
        string ArtifactRegistryUrl,
        string LatestVersion,
        string MinSupportedVersion,
        string TargetPlatform,
        string LicenseType,
        string BuildPipelineUrl,
        string ChangelogUrl,
        int KnownConsumerCount);
}
