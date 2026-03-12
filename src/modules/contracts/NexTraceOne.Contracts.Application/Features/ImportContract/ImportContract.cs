using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.Errors;

namespace NexTraceOne.Contracts.Application.Features.ImportContract;

/// <summary>
/// Feature: ImportContract — importa a primeira versão de um contrato OpenAPI para um ativo de API.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ImportContract
{
    /// <summary>Comando de importação de versão de contrato.</summary>
    public sealed record Command(
        Guid ApiAssetId,
        string SemVer,
        string SpecContent,
        string Format,
        string ImportedFrom) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de importação de contrato.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.SemVer).NotEmpty().MaximumLength(50);
            RuleFor(x => x.SpecContent).NotEmpty();
            RuleFor(x => x.Format).NotEmpty()
                .Must(f => f is "json" or "yaml")
                .WithMessage("Format must be 'json' or 'yaml'.");
            RuleFor(x => x.ImportedFrom).NotEmpty().MaximumLength(500);
        }
    }

    /// <summary>Handler que importa uma nova versão de contrato OpenAPI.</summary>
    public sealed class Handler(
        IContractVersionRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var existing = await repository.GetByApiAssetAndSemVerAsync(request.ApiAssetId, request.SemVer, cancellationToken);
            if (existing is not null)
                return ContractsErrors.AlreadyExists(request.SemVer, request.ApiAssetId.ToString());

            var result = ContractVersion.Import(
                request.ApiAssetId,
                request.SemVer,
                request.SpecContent,
                request.Format,
                request.ImportedFrom);

            if (result.IsFailure)
                return result.Error;

            var version = result.Value;
            repository.Add(version);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                version.Id.Value,
                version.ApiAssetId,
                version.SemVer,
                version.Format,
                dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta da importação de versão de contrato.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        Guid ApiAssetId,
        string SemVer,
        string Format,
        DateTimeOffset CreatedAt);
}

