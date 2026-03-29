using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Domain.LegacyAssets.Enums;
using NexTraceOne.Catalog.Domain.LegacyAssets.Errors;

namespace NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterDb2Artifact;

/// <summary>
/// Feature: RegisterDb2Artifact — regista um novo artefacto DB2 no catálogo legacy.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RegisterDb2Artifact
{
    /// <summary>Comando de registo de um artefacto DB2.</summary>
    public sealed record Command(
        string Name,
        Guid SystemId,
        string ArtifactType,
        string? SchemaName,
        string? DatabaseName) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de registo de artefacto DB2.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.SystemId).NotEmpty();
            RuleFor(x => x.ArtifactType)
                .NotEmpty()
                .Must(value => Enum.TryParse<Db2ArtifactType>(value, ignoreCase: true, out _))
                .WithMessage("ArtifactType must be a valid DB2 artifact type (Table, View, StoredProcedure, Index, Tablespace, Package).");
        }
    }

    /// <summary>Handler que regista um novo artefacto DB2 no catálogo legacy.</summary>
    public sealed class Handler(
        IDb2ArtifactRepository db2ArtifactRepository,
        IMainframeSystemRepository mainframeSystemRepository,
        ILegacyAssetsUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var systemId = MainframeSystemId.From(request.SystemId);
            var system = await mainframeSystemRepository.GetByIdAsync(systemId, cancellationToken);
            if (system is null)
            {
                return LegacyAssetsErrors.MainframeSystemNotFound(request.SystemId);
            }

            var existing = await db2ArtifactRepository.GetByNameAndSystemAsync(request.Name, systemId, cancellationToken);
            if (existing is not null)
            {
                return LegacyAssetsErrors.Db2ArtifactAlreadyExists(request.Name, request.SystemId);
            }

            var artifactType = Enum.Parse<Db2ArtifactType>(request.ArtifactType, ignoreCase: true);
            var artifact = Db2Artifact.Create(request.Name, systemId, artifactType);

            if (request.SchemaName is not null || request.DatabaseName is not null)
            {
                artifact.UpdateDetails(
                    artifact.DisplayName,
                    artifact.Description,
                    request.SchemaName ?? string.Empty,
                    artifact.TablespaceName,
                    request.DatabaseName ?? string.Empty,
                    artifact.Criticality,
                    artifact.LifecycleStatus);
            }

            db2ArtifactRepository.Add(artifact);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                artifact.Id.Value,
                artifact.Name,
                artifact.SystemId.Value,
                artifact.ArtifactType.ToString());
        }
    }

    /// <summary>Resposta do registo do artefacto DB2.</summary>
    public sealed record Response(
        Guid Id,
        string Name,
        Guid SystemId,
        string ArtifactType);
}
