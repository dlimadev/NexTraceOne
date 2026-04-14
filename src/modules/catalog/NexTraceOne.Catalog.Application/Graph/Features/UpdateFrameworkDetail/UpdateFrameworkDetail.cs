using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.UpdateFrameworkDetail;

/// <summary>
/// Feature: UpdateFrameworkDetail — atualiza os detalhes de Framework/SDK de um serviço.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class UpdateFrameworkDetail
{
    /// <summary>Comando de atualização dos detalhes de framework.</summary>
    public sealed record Command(
        Guid ServiceAssetId,
        string PackageName,
        string Language,
        string PackageManager,
        string? ArtifactRegistryUrl = null,
        string? LatestVersion = null,
        string? MinSupportedVersion = null,
        string? TargetPlatform = null,
        string? LicenseType = null,
        string? BuildPipelineUrl = null,
        string? ChangelogUrl = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de atualização de framework.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceAssetId).NotEmpty();
            RuleFor(x => x.PackageName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Language).NotEmpty().MaximumLength(100);
            RuleFor(x => x.PackageManager).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ArtifactRegistryUrl).MaximumLength(1000).When(x => x.ArtifactRegistryUrl is not null);
            RuleFor(x => x.LatestVersion).MaximumLength(100).When(x => x.LatestVersion is not null);
            RuleFor(x => x.MinSupportedVersion).MaximumLength(100).When(x => x.MinSupportedVersion is not null);
            RuleFor(x => x.TargetPlatform).MaximumLength(200).When(x => x.TargetPlatform is not null);
            RuleFor(x => x.LicenseType).MaximumLength(100).When(x => x.LicenseType is not null);
            RuleFor(x => x.BuildPipelineUrl).MaximumLength(1000).When(x => x.BuildPipelineUrl is not null);
            RuleFor(x => x.ChangelogUrl).MaximumLength(1000).When(x => x.ChangelogUrl is not null);
        }
    }

    /// <summary>Handler que atualiza os detalhes de framework de um serviço existente.</summary>
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

            detail.Update(
                request.PackageName,
                request.Language,
                request.PackageManager,
                request.ArtifactRegistryUrl,
                request.LatestVersion,
                request.MinSupportedVersion,
                request.TargetPlatform,
                request.LicenseType,
                request.BuildPipelineUrl,
                request.ChangelogUrl);

            await unitOfWork.CommitAsync(cancellationToken);

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

    /// <summary>Resposta da atualização de detalhes de framework.</summary>
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
