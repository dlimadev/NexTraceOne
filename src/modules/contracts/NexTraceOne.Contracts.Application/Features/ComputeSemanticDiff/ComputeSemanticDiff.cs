using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Enums;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.Errors;
using NexTraceOne.Contracts.Domain.Services;
using NexTraceOne.Contracts.Domain.ValueObjects;

namespace NexTraceOne.Contracts.Application.Features.ComputeSemanticDiff;

/// <summary>
/// Feature: ComputeSemanticDiff — computa o diff semântico entre duas versões de contrato.
/// Suporta múltiplos protocolos (OpenAPI, Swagger, AsyncAPI, WSDL) via delegação ao
/// <see cref="ContractDiffCalculator"/>, que seleciona o calculador específico do protocolo.
/// Detecta mudanças breaking, aditivas e non-breaking e sugere a próxima versão semântica.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ComputeSemanticDiff
{
    /// <summary>Query de computação de diff semântico entre versões de contrato.</summary>
    public sealed record Query(Guid BaseVersionId, Guid TargetVersionId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de diff semântico.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.BaseVersionId).NotEmpty();
            RuleFor(x => x.TargetVersionId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que orquestra a computação do diff semântico multi-protocolo.
    /// Carrega as versões do repositório, delega o cálculo ao <see cref="ContractDiffCalculator"/>
    /// que seleciona o calculador específico do protocolo da versão alvo,
    /// e persiste o resultado na versão alvo.
    /// </summary>
    public sealed class Handler(
        IContractVersionRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var baseVersion = await repository.GetByIdAsync(ContractVersionId.From(request.BaseVersionId), cancellationToken);
            if (baseVersion is null)
                return ContractsErrors.ContractVersionNotFound(request.BaseVersionId.ToString());

            var targetVersion = await repository.GetByIdAsync(ContractVersionId.From(request.TargetVersionId), cancellationToken);
            if (targetVersion is null)
                return ContractsErrors.ContractVersionNotFound(request.TargetVersionId.ToString());

            var diffResult = ContractDiffCalculator.ComputeDiff(
                baseVersion.SpecContent, targetVersion.SpecContent, targetVersion.Protocol);

            var baseSemVer = SemanticVersion.Parse(baseVersion.SemVer);
            var suggestedSemVer = baseSemVer is null
                ? baseVersion.SemVer
                : diffResult.ChangeLevel switch
                {
                    ChangeLevel.Breaking => baseSemVer.BumpMajor().ToString(),
                    ChangeLevel.Additive => baseSemVer.BumpMinor().ToString(),
                    _ => baseSemVer.BumpPatch().ToString()
                };

            var diff = ContractDiff.Create(
                targetVersion.Id,
                baseVersion.Id,
                targetVersion.Id,
                targetVersion.ApiAssetId,
                diffResult.ChangeLevel,
                diffResult.BreakingChanges,
                diffResult.NonBreakingChanges,
                diffResult.AdditiveChanges,
                suggestedSemVer,
                dateTimeProvider.UtcNow);

            targetVersion.AddDiff(diff);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                diff.Id.Value,
                request.BaseVersionId,
                request.TargetVersionId,
                diffResult.ChangeLevel,
                suggestedSemVer,
                diffResult.BreakingChanges,
                diffResult.NonBreakingChanges,
                diffResult.AdditiveChanges);
        }
    }

    /// <summary>Resposta do diff semântico entre versões de contrato.</summary>
    public sealed record Response(
        Guid DiffId,
        Guid BaseVersionId,
        Guid TargetVersionId,
        ChangeLevel ChangeLevel,
        string SuggestedSemVer,
        IReadOnlyList<ChangeEntry> BreakingChanges,
        IReadOnlyList<ChangeEntry> NonBreakingChanges,
        IReadOnlyList<ChangeEntry> AdditiveChanges);
}

