using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ListRuleViolations;

/// <summary>
/// Feature: ListRuleViolations — lista todas as violações de ruleset detectadas
/// em uma versão específica de contrato.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListRuleViolations
{
    /// <summary>Query que solicita as violações de uma versão de contrato.</summary>
    public sealed record Query(Guid ContractVersionId) : IQuery<Response>;

    /// <summary>Valida que o identificador da versão de contrato não está vazio.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que carrega a versão de contrato e retorna suas violações de ruleset.
    /// Retorna erro NotFound se a versão não existir.
    /// </summary>
    public sealed class Handler(IContractVersionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var contractVersion = await repository.GetByIdAsync(
                ContractVersionId.From(request.ContractVersionId), cancellationToken);

            if (contractVersion is null)
                return ContractsErrors.ContractVersionNotFound(request.ContractVersionId.ToString());

            var violations = contractVersion.RuleViolations
                .Select(v => new RuleViolationSummary(
                    v.Id.Value,
                    v.RuleName,
                    v.Severity,
                    v.Message,
                    v.Path))
                .ToList()
                .AsReadOnly();

            return new Response(request.ContractVersionId, violations);
        }
    }

    /// <summary>
    /// Resumo de uma violação de ruleset para exibição na API.
    /// </summary>
    public sealed record RuleViolationSummary(
        Guid Id,
        string RuleName,
        string Severity,
        string Message,
        string Path);

    /// <summary>
    /// Resposta contendo a lista de violações de uma versão de contrato.
    /// </summary>
    public sealed record Response(
        Guid ContractVersionId,
        IReadOnlyList<RuleViolationSummary> Violations);
}
