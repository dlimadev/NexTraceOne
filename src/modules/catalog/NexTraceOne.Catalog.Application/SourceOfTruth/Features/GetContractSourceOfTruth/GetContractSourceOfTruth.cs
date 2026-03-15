using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.SourceOfTruth.Abstractions;
using NexTraceOne.Catalog.Domain.SourceOfTruth.Enums;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Domain.Entities;

namespace NexTraceOne.Catalog.Application.SourceOfTruth.Features.GetContractSourceOfTruth;

/// <summary>
/// Feature: GetContractSourceOfTruth — obtém a visão consolidada de Source of Truth
/// para um contrato, combinando identidade, serviço vinculado, artefatos,
/// referências documentais e metadados de governança.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetContractSourceOfTruth
{
    /// <summary>Query de Source of Truth consolidado de um contrato.</summary>
    public sealed record Query(Guid ContractVersionId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
        }
    }

    /// <summary>Handler que compõe a visão consolidada de Source of Truth de um contrato.</summary>
    public sealed class Handler(
        IContractVersionRepository contractRepository,
        ILinkedReferenceRepository referenceRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var contract = await contractRepository.GetByIdAsync(
                ContractVersionId.From(request.ContractVersionId), cancellationToken);

            if (contract is null)
                return Error.NotFound("SourceOfTruth.ContractNotFound", "Contract '{0}' not found", request.ContractVersionId);

            // Obter referências vinculadas ao contrato
            var references = await referenceRepository.ListByAssetAsync(
                request.ContractVersionId, LinkedAssetType.Contract, cancellationToken);

            var referenceSummaries = references.Where(r => r.IsActive).Select(r =>
                new ReferenceSummaryItem(r.Id.Value, r.ReferenceType.ToString(),
                    r.Title, r.Description, r.Url)).ToList();

            // Compor visão de governança
            var governance = new GovernanceSummary(
                LifecycleState: contract.LifecycleState.ToString(),
                IsLocked: contract.IsLocked,
                LockedAt: contract.LockedAt,
                LockedBy: contract.LockedBy,
                IsSigned: contract.Signature is not null,
                DeprecationNotice: contract.DeprecationNotice,
                DeprecationDate: contract.DeprecationDate,
                SunsetDate: contract.SunsetDate);

            return new Response(
                ContractVersionId: contract.Id.Value,
                ApiAssetId: contract.ApiAssetId,
                SemVer: contract.SemVer,
                Protocol: contract.Protocol.ToString(),
                Format: contract.Format,
                ImportedFrom: contract.ImportedFrom,
                Governance: governance,
                References: referenceSummaries,
                ArtifactCount: contract.Artifacts.Count,
                DiffCount: contract.Diffs.Count,
                ViolationCount: contract.RuleViolations.Count,
                HasDocumentation: references.Any(r =>
                    r.ReferenceType == LinkedReferenceType.Documentation && r.IsActive),
                HasRelatedChanges: references.Any(r =>
                    r.ReferenceType == LinkedReferenceType.Changelog && r.IsActive));
        }
    }

    /// <summary>Resumo de governança do contrato.</summary>
    public sealed record GovernanceSummary(
        string LifecycleState, bool IsLocked, DateTimeOffset? LockedAt,
        string? LockedBy, bool IsSigned, string? DeprecationNotice,
        DateTimeOffset? DeprecationDate, DateTimeOffset? SunsetDate);

    /// <summary>Referência vinculada ao contrato.</summary>
    public sealed record ReferenceSummaryItem(
        Guid ReferenceId, string ReferenceType, string Title,
        string Description, string? Url);

    /// <summary>Resposta consolidada de Source of Truth do contrato.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        Guid ApiAssetId,
        string SemVer,
        string Protocol,
        string Format,
        string ImportedFrom,
        GovernanceSummary Governance,
        IReadOnlyList<ReferenceSummaryItem> References,
        int ArtifactCount,
        int DiffCount,
        int ViolationCount,
        bool HasDocumentation,
        bool HasRelatedChanges);
}
