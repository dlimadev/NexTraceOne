using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetContractVerificationDetail;

/// <summary>
/// Feature: GetContractVerificationDetail — obtém o detalhe completo de uma verificação
/// de contrato pelo seu identificador.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class GetContractVerificationDetail
{
    /// <summary>Query para obter o detalhe de uma verificação de contrato.</summary>
    public sealed record Query(Guid VerificationId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de detalhe de verificação.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.VerificationId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que obtém o detalhe completo de uma verificação de contrato.
    /// </summary>
    public sealed class Handler(IContractVerificationRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var verification = await repository.GetByIdAsync(
                ContractVerificationId.From(request.VerificationId), cancellationToken);

            if (verification is null)
                return ContractsErrors.VerificationNotFound(request.VerificationId.ToString());

            return new Response(
                verification.Id.Value,
                verification.TenantId,
                verification.ApiAssetId,
                verification.ServiceName,
                verification.ContractVersionId,
                verification.SpecContentHash,
                verification.Status.ToString(),
                verification.BreakingChangesCount,
                verification.NonBreakingChangesCount,
                verification.AdditiveChangesCount,
                verification.DiffDetails,
                verification.ComplianceViolations,
                verification.SourceSystem,
                verification.SourceBranch,
                verification.CommitSha,
                verification.PipelineId,
                verification.EnvironmentName,
                verification.VerifiedAt,
                verification.CreatedAt,
                verification.CreatedBy);
        }
    }

    /// <summary>Resposta com o detalhe completo de uma verificação de contrato.</summary>
    public sealed record Response(
        Guid VerificationId,
        string TenantId,
        string ApiAssetId,
        string ServiceName,
        Guid? ContractVersionId,
        string SpecContentHash,
        string Status,
        int BreakingChangesCount,
        int NonBreakingChangesCount,
        int AdditiveChangesCount,
        string DiffDetails,
        string ComplianceViolations,
        string SourceSystem,
        string? SourceBranch,
        string? CommitSha,
        string? PipelineId,
        string? EnvironmentName,
        DateTimeOffset VerifiedAt,
        DateTimeOffset CreatedAt,
        string CreatedBy);
}
