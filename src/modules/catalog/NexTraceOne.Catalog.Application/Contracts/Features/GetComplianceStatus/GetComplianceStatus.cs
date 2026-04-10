using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetComplianceStatus;

/// <summary>
/// Feature: GetComplianceStatus — obtém o estado de compliance de uma versão de contrato.
/// Consulta os resultados de avaliação para exibição em dashboards e promotion gates.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class GetComplianceStatus
{
    /// <summary>Query para obter o estado de compliance de uma versão de contrato.</summary>
    public sealed record Query(string ContractVersionId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de estado de compliance.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que obtém o estado consolidado de compliance de uma versão de contrato.
    /// </summary>
    public sealed class Handler(IContractComplianceResultRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var results = await repository.ListByContractVersionAsync(
                request.ContractVersionId, cancellationToken);

            if (results.Count == 0)
                return ContractsErrors.ComplianceResultNotFound(request.ContractVersionId);

            var items = results.Select(r => new ComplianceResultSummary(
                r.Id.Value,
                r.GateId,
                r.Result,
                r.Violations,
                r.EvidencePackId,
                r.EvaluatedAt)).ToList();

            var hasBlock = results.Any(r => r.Result == ComplianceEvaluationResult.Block);
            var hasWarn = results.Any(r => r.Result == ComplianceEvaluationResult.Warn);

            var overallResult = hasBlock
                ? ComplianceEvaluationResult.Block
                : hasWarn
                    ? ComplianceEvaluationResult.Warn
                    : ComplianceEvaluationResult.Pass;

            return new Response(request.ContractVersionId, overallResult, items);
        }
    }

    /// <summary>Resumo de um resultado de compliance individual.</summary>
    public sealed record ComplianceResultSummary(
        Guid ResultId,
        Guid GateId,
        ComplianceEvaluationResult Result,
        string? Violations,
        string? EvidencePackId,
        DateTimeOffset EvaluatedAt);

    /// <summary>Resposta consolidada do estado de compliance de uma versão de contrato.</summary>
    public sealed record Response(
        string ContractVersionId,
        ComplianceEvaluationResult OverallResult,
        IReadOnlyList<ComplianceResultSummary> Results);
}
