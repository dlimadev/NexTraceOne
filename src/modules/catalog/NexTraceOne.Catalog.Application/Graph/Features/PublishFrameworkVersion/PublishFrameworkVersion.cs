using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.PublishFrameworkVersion;

/// <summary>
/// Feature: PublishFrameworkVersion — publica uma nova versão de um Framework/SDK.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class PublishFrameworkVersion
{
    /// <summary>Comando de publicação de nova versão do framework.</summary>
    public sealed record Command(
        Guid ServiceAssetId,
        string Version) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de publicação de versão.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceAssetId).NotEmpty();
            RuleFor(x => x.Version).NotEmpty().MaximumLength(100);
        }
    }

    /// <summary>Handler que publica nova versão do framework.</summary>
    public sealed class Handler(
        IFrameworkAssetDetailRepository frameworkRepository,
        ICatalogGraphUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var detail = await frameworkRepository.GetByServiceAssetIdAsync(
                ServiceAssetId.From(request.ServiceAssetId), cancellationToken);

            if (detail is null)
                return CatalogGraphErrors.FrameworkDetailNotFound(request.ServiceAssetId);

            detail.PublishVersion(request.Version);

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                detail.Id.Value,
                detail.ServiceAssetId.Value,
                detail.PackageName,
                detail.LatestVersion);
        }
    }

    /// <summary>Resposta da publicação de versão do framework.</summary>
    public sealed record Response(
        Guid FrameworkAssetDetailId,
        Guid ServiceAssetId,
        string PackageName,
        string LatestVersion);
}
