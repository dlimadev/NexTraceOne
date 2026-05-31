using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Application.DependencyGovernance.Abstractions;

namespace NexTraceOne.Catalog.Application.DependencyGovernance.Features.EnrichServiceDependencies;

/// <summary>
/// Feature: EnrichServiceDependencies — consulta registries públicos (OSV, NuGet.org)
/// para enriquecer um perfil de dependências com vulnerabilidades, versões e licenças.
/// </summary>
public static class EnrichServiceDependencies
{
    public sealed record Command(Guid ServiceId) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty();
        }
    }

    public sealed class Handler(
        IServiceDependencyProfileRepository repository,
        IDependencyEnrichmentService enrichmentService,
        IDependencyGovernanceUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var profile = await repository.FindByServiceIdAsync(request.ServiceId, cancellationToken);
            if (profile is null)
                return Error.NotFound("DependencyGovernance.ProfileNotFound",
                    $"Dependency profile not found for service {request.ServiceId}.");

            await enrichmentService.EnrichAsync(profile, cancellationToken);
            await repository.UpdateAsync(profile, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                ProfileId: profile.Id.Value,
                HealthScore: profile.HealthScore,
                VulnerabilityCount: profile.Dependencies.Sum(d => d.Vulnerabilities.Count),
                OutdatedCount: profile.Dependencies.Count(d => d.IsOutdated)));
        }
    }

    public sealed record Response(
        Guid ProfileId,
        int HealthScore,
        int VulnerabilityCount,
        int OutdatedCount);
}
