using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Domain.LegacyAssets.Errors;
using NexTraceOne.Catalog.Domain.LegacyAssets.ValueObjects;

namespace NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterCopybook;

/// <summary>
/// Feature: RegisterCopybook — regista um novo copybook COBOL no catálogo legacy.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RegisterCopybook
{
    /// <summary>Comando de registo de um copybook COBOL.</summary>
    public sealed record Command(
        string Name,
        Guid SystemId,
        int FieldCount,
        int TotalLength,
        string? RecordFormat,
        string? Version) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de registo de copybook.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.SystemId).NotEmpty();
            RuleFor(x => x.FieldCount).GreaterThanOrEqualTo(0);
            RuleFor(x => x.TotalLength).GreaterThanOrEqualTo(0);
        }
    }

    /// <summary>Handler que regista um novo copybook COBOL no catálogo legacy.</summary>
    public sealed class Handler(
        ICopybookRepository copybookRepository,
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

            var existing = await copybookRepository.GetByNameAndSystemAsync(request.Name, systemId, cancellationToken);
            if (existing is not null)
            {
                return LegacyAssetsErrors.CopybookAlreadyExists(request.Name, request.SystemId);
            }

            var layout = CopybookLayout.Create(request.FieldCount, request.TotalLength, request.RecordFormat);
            var copybook = Copybook.Create(request.Name, systemId, layout);

            if (request.Version is not null)
            {
                copybook.UpdateDetails(
                    copybook.DisplayName,
                    copybook.Description,
                    request.Version,
                    copybook.SourceLibrary,
                    copybook.RawContent,
                    copybook.Criticality,
                    copybook.LifecycleStatus);
            }

            copybookRepository.Add(copybook);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                copybook.Id.Value,
                copybook.Name,
                copybook.SystemId.Value,
                copybook.Layout.FieldCount,
                copybook.Layout.TotalLength);
        }
    }

    /// <summary>Resposta do registo do copybook COBOL.</summary>
    public sealed record Response(
        Guid Id,
        string Name,
        Guid SystemId,
        int FieldCount,
        int TotalLength);
}
