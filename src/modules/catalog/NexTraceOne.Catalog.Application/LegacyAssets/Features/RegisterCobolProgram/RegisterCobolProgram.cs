using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Domain.LegacyAssets.Errors;

namespace NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterCobolProgram;

/// <summary>
/// Feature: RegisterCobolProgram — regista um novo programa COBOL no catálogo legacy.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RegisterCobolProgram
{
    /// <summary>Comando de registo de um programa COBOL.</summary>
    public sealed record Command(
        string Name,
        Guid SystemId,
        string? CompilerVersion,
        string? SourceLibrary,
        string? LoadModule) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de registo de programa COBOL.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.SystemId).NotEmpty();
        }
    }

    /// <summary>Handler que regista um novo programa COBOL no catálogo legacy.</summary>
    public sealed class Handler(
        ICobolProgramRepository cobolProgramRepository,
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

            var existing = await cobolProgramRepository.GetByNameAndSystemAsync(request.Name, systemId, cancellationToken);
            if (existing is not null)
            {
                return LegacyAssetsErrors.CobolProgramAlreadyExists(request.Name, request.SystemId);
            }

            var program = CobolProgram.Create(request.Name, systemId);

            if (request.CompilerVersion is not null || request.SourceLibrary is not null || request.LoadModule is not null)
            {
                program.UpdateDetails(
                    program.DisplayName,
                    program.Description,
                    request.CompilerVersion ?? string.Empty,
                    null,
                    request.SourceLibrary ?? string.Empty,
                    request.LoadModule ?? string.Empty,
                    program.Criticality,
                    program.LifecycleStatus);
            }

            cobolProgramRepository.Add(program);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                program.Id.Value,
                program.Name,
                program.SystemId.Value);
        }
    }

    /// <summary>Resposta do registo do programa COBOL.</summary>
    public sealed record Response(
        Guid Id,
        string Name,
        Guid SystemId);
}
