using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Domain.Errors;

namespace NexTraceOne.Contracts.Application.Features.ClassifyBreakingChange;

/// <summary>
/// Feature: ClassifyBreakingChange — classifica o nível de mudança de uma versão de contrato com base no seu diff mais recente.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ClassifyBreakingChange
{
    /// <summary>Query de classificação de mudança de versão de contrato.</summary>
    public sealed record Query(Guid ContractVersionId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de classificação de mudança.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
        }
    }

    /// <summary>Handler que classifica o nível de mudança a partir do diff mais recente da versão de contrato.</summary>
    public sealed class Handler(IContractVersionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var version = await repository.GetByIdAsync(
                Domain.Entities.ContractVersionId.From(request.ContractVersionId), cancellationToken);

            if (version is null)
                return ContractsErrors.ContractVersionNotFound(request.ContractVersionId.ToString());

            var latestDiff = version.Diffs.OrderByDescending(d => d.ComputedAt).FirstOrDefault();
            if (latestDiff is null)
                return ContractsErrors.DiffNotFound(request.ContractVersionId.ToString());

            return new Response(
                latestDiff.ChangeLevel,
                latestDiff.BreakingChanges.Count,
                latestDiff.AdditiveChanges.Count,
                latestDiff.NonBreakingChanges.Count,
                latestDiff.SuggestedSemVer);
        }
    }

    /// <summary>Resposta da classificação de nível de mudança da versão de contrato.</summary>
    public sealed record Response(
        ChangeLevel ChangeLevel,
        int BreakingChangeCount,
        int AdditiveChangeCount,
        int NonBreakingChangeCount,
        string? SuggestedSemVer);
}

