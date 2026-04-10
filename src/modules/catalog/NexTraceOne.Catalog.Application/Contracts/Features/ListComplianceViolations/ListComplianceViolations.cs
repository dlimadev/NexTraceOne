using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ListComplianceViolations;

/// <summary>
/// Feature: ListComplianceViolations — lista resultados de compliance por versão de contrato.
/// Permite visualizar todas as avaliações e violações detetadas para auditoria e governança.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class ListComplianceViolations
{
    /// <summary>Query para listar violações de compliance por versão de contrato.</summary>
    public sealed record Query(string ContractVersionId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de listagem de violações.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que lista todos os resultados de compliance para uma versão de contrato.
    /// </summary>
    public sealed class Handler(IContractComplianceResultRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var results = await repository.ListByContractVersionAsync(
                request.ContractVersionId, cancellationToken);

            var items = results.Select(r => new ComplianceViolationItem(
                r.Id.Value,
                r.GateId,
                r.ContractVersionId,
                r.ChangeId,
                r.Result,
                r.Violations,
                r.EvidencePackId,
                r.EvaluatedAt)).ToList();

            return new Response(items);
        }
    }

    /// <summary>Item de violação de compliance para listagem.</summary>
    public sealed record ComplianceViolationItem(
        Guid ResultId,
        Guid GateId,
        string ContractVersionId,
        string? ChangeId,
        ComplianceEvaluationResult Result,
        string? Violations,
        string? EvidencePackId,
        DateTimeOffset EvaluatedAt);

    /// <summary>Resposta da listagem de violações de compliance.</summary>
    public sealed record Response(IReadOnlyList<ComplianceViolationItem> Items);
}
