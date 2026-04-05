using System.Text.Json;
using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;

namespace NexTraceOne.Catalog.Application.DependencyGovernance.Features.GenerateSbom;

/// <summary>
/// Feature: GenerateSbom — gera SBOM no formato solicitado e persiste no perfil do serviço.
/// </summary>
public static class GenerateSbom
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };
    public sealed record Command(Guid ServiceId, SbomFormat Format = SbomFormat.CycloneDx) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty();
            RuleFor(x => x.Format).IsInEnum();
        }
    }

    public sealed class Handler(
        IServiceDependencyProfileRepository repository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            var profile = await repository.FindByServiceIdAsync(request.ServiceId, cancellationToken);
            if (profile is null)
                return Error.NotFound("DependencyGovernance.ProfileNotFound",
                    $"No dependency profile found for service '{request.ServiceId}'.");

            var sbomContent = request.Format == SbomFormat.CycloneDx
                ? GenerateCycloneDx(profile.ServiceId, profile.Dependencies)
                : GenerateSpdx(profile.ServiceId, profile.Dependencies);

            profile.UpdateSbomContent(sbomContent, request.Format);
            await repository.UpdateAsync(profile, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(SbomContent: sbomContent, Format: request.Format));
        }

        private static string GenerateCycloneDx(
            Guid serviceId,
            System.Collections.Generic.IReadOnlyList<NexTraceOne.Catalog.Domain.DependencyGovernance.Entities.PackageDependency> deps)
        {
            var components = deps.Select(d => new
            {
                name = d.PackageName,
                version = d.Version,
                purl = $"pkg:{d.Ecosystem.ToString().ToLowerInvariant()}/{d.PackageName}@{d.Version}"
            }).ToList();

            return JsonSerializer.Serialize(new
            {
                bomFormat = "CycloneDX",
                specVersion = "1.4",
                serialNumber = $"urn:uuid:{Guid.NewGuid()}",
                components
            }, SerializerOptions);
        }

        private static string GenerateSpdx(
            Guid serviceId,
            System.Collections.Generic.IReadOnlyList<NexTraceOne.Catalog.Domain.DependencyGovernance.Entities.PackageDependency> deps)
        {
            var packages = deps.Select(d => new
            {
                name = d.PackageName,
                versionInfo = d.Version,
                downloadLocation = "NOASSERTION"
            }).ToList();

            return JsonSerializer.Serialize(new
            {
                spdxVersion = "SPDX-2.3",
                documentNamespace = $"https://nextraceone/{serviceId}/{Guid.NewGuid()}",
                packages
            }, SerializerOptions);
        }
    }

    public sealed record Response(string SbomContent, SbomFormat Format);
}
