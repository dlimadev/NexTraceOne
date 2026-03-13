using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Enums;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Domain.Errors;
using NexTraceOne.Contracts.Domain.ValueObjects;

namespace NexTraceOne.Contracts.Application.Features.SuggestSemanticVersion;

/// <summary>
/// Feature: SuggestSemanticVersion — sugere a próxima versão semântica para um ativo de API com base no nível de mudança esperado.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class SuggestSemanticVersion
{
    /// <summary>Query de sugestão de próxima versão semântica.</summary>
    public sealed record Query(Guid ApiAssetId, ChangeLevel ExpectedChangeLevel) : IQuery<Response>;

    /// <summary>Valida a entrada da query de sugestão de versão semântica.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
        }
    }

    /// <summary>Handler que sugere a próxima versão semântica baseada na versão atual e no nível de mudança esperado.</summary>
    public sealed class Handler(IContractVersionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var latest = await repository.GetLatestByApiAssetAsync(request.ApiAssetId, cancellationToken);
            if (latest is null)
                return ContractsErrors.NoPreviousVersion(request.ApiAssetId.ToString());

            var currentSemVer = SemanticVersion.Parse(latest.SemVer);
            if (currentSemVer is null)
                return ContractsErrors.InvalidSemVer(latest.SemVer);

            var suggested = request.ExpectedChangeLevel switch
            {
                ChangeLevel.Breaking => currentSemVer.BumpMajor(),
                ChangeLevel.Additive => currentSemVer.BumpMinor(),
                _ => currentSemVer.BumpPatch()
            };

            return new Response(latest.SemVer, suggested.ToString(), request.ExpectedChangeLevel);
        }
    }

    /// <summary>Resposta da sugestão de próxima versão semântica.</summary>
    public sealed record Response(
        string CurrentVersion,
        string SuggestedVersion,
        ChangeLevel ChangeLevel);
}

