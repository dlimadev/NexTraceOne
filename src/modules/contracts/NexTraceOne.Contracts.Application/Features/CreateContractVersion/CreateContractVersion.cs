using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.Errors;

namespace NexTraceOne.Contracts.Application.Features.CreateContractVersion;

/// <summary>
/// Feature: CreateContractVersion — cria uma nova versão de contrato a partir de uma versão anterior.
/// Exige que o ativo de API já possua pelo menos uma versão registrada.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class CreateContractVersion
{
    /// <summary>Comando de criação de nova versão de contrato.</summary>
    public sealed record Command(
        Guid ApiAssetId,
        string SemVer,
        string SpecContent,
        string Format,
        string ImportedFrom) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de criação de versão de contrato.</summary>
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

    /// <summary>Handler que cria uma nova versão subsequente de contrato OpenAPI.</summary>
    public sealed class Handler(
        IContractVersionRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var previous = await repository.GetLatestByApiAssetAsync(request.ApiAssetId, cancellationToken);
            if (previous is null)
                return ContractsErrors.NoPreviousVersion(request.ApiAssetId.ToString());

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

    /// <summary>Resposta da criação de nova versão de contrato.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        Guid ApiAssetId,
        string SemVer,
        string Format,
        DateTimeOffset CreatedAt);
}

