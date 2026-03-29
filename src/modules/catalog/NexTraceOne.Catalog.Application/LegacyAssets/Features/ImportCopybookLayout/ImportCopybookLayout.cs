using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Domain.LegacyAssets.Errors;
using NexTraceOne.Catalog.Domain.LegacyAssets.Services;
using NexTraceOne.Catalog.Domain.LegacyAssets.ValueObjects;

namespace NexTraceOne.Catalog.Application.LegacyAssets.Features.ImportCopybookLayout;

/// <summary>
/// Feature: ImportCopybookLayout — faz parse de texto COBOL e cria versão do copybook.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ImportCopybookLayout
{
    /// <summary>Comando de importação de layout de copybook COBOL.</summary>
    public sealed record Command(
        Guid CopybookId,
        string CopybookText,
        string VersionLabel) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de importação de layout.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CopybookId).NotEmpty();
            RuleFor(x => x.CopybookText).NotEmpty().MaximumLength(500_000);
            RuleFor(x => x.VersionLabel).NotEmpty().MaximumLength(100);
        }
    }

    /// <summary>Handler que importa o layout de um copybook COBOL, criando uma nova versão.</summary>
    public sealed class Handler(
        ICopybookRepository copybookRepository,
        ICopybookVersionRepository copybookVersionRepository,
        ILegacyAssetsUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var copybookId = CopybookId.From(request.CopybookId);
            var copybook = await copybookRepository.GetByIdAsync(copybookId, cancellationToken);
            if (copybook is null)
                return LegacyAssetsErrors.CopybookNotFound(request.CopybookId);

            CopybookParsedLayout layout;
            try
            {
                layout = CopybookParser.Parse(request.CopybookText);
            }
            catch (ArgumentException ex)
            {
                return LegacyAssetsErrors.CopybookParseFailed(ex.Message);
            }

            var version = CopybookVersion.Create(
                copybookId, request.VersionLabel, request.CopybookText,
                layout.Fields.Count, layout.TotalLength, layout.RecordFormat);
            copybookVersionRepository.Add(version);

            var updatedLayout = CopybookLayout.Create(
                layout.Fields.Count, layout.TotalLength, layout.RecordFormat);
            copybook.UpdateLayout(updatedLayout);
            copybook.UpdateDetails(
                copybook.DisplayName, copybook.Description,
                request.VersionLabel, copybook.SourceLibrary,
                request.CopybookText,
                copybook.Criticality, copybook.LifecycleStatus);

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                version.Id.Value, copybookId.Value,
                request.VersionLabel, layout.Fields.Count,
                layout.TotalLength, layout.RecordFormat);
        }
    }

    /// <summary>Resposta da importação do layout de copybook COBOL.</summary>
    public sealed record Response(
        Guid VersionId, Guid CopybookId, string VersionLabel,
        int FieldCount, int TotalLength, string RecordFormat);
}
