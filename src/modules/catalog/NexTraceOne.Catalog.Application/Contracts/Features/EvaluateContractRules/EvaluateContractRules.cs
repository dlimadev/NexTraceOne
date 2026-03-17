using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;
using NexTraceOne.Catalog.Domain.Contracts.Services;

namespace NexTraceOne.Catalog.Application.Contracts.Features.EvaluateContractRules;

/// <summary>
/// Feature: EvaluateContractRules — avalia regras determinísticas de conformidade sobre um contrato.
/// Constrói o modelo canônico e aplica o motor de regras para detectar violações de
/// naming conventions, segurança, documentação, versionamento e completude.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class EvaluateContractRules
{
    /// <summary>Query para avaliação de regras determinísticas.</summary>
    public sealed record Query(Guid ContractVersionId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de avaliação de regras.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que orquestra a avaliação de regras determinísticas.
    /// Carrega a versão, constrói o modelo canônico e aplica o motor de regras.
    /// </summary>
    public sealed class Handler(
        IContractVersionRepository repository,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var version = await repository.GetByIdAsync(
                ContractVersionId.From(request.ContractVersionId), cancellationToken);
            if (version is null)
                return ContractsErrors.ContractVersionNotFound(request.ContractVersionId.ToString());

            var canonicalModel = CanonicalModelBuilder.Build(version.SpecContent, version.Protocol);
            var violations = ContractRuleEngine.Evaluate(version.Id, canonicalModel, version.SemVer, version.Protocol);

            var violationResponses = violations.Select(v => new ViolationResponse(
                v.RuleName,
                v.Severity,
                v.Message,
                v.Path,
                v.SuggestedFix)).ToList();

            return new Response(
                request.ContractVersionId,
                version.Protocol.ToString(),
                violations.Count,
                violations.Count(v => v.Severity == "Error"),
                violations.Count(v => v.Severity == "Warning"),
                violations.Count(v => v.Severity == "Info"),
                violationResponses);
        }
    }

    /// <summary>Resposta de uma violação individual de regra.</summary>
    public sealed record ViolationResponse(
        string RuleName,
        string Severity,
        string Message,
        string Path,
        string? SuggestedFix);

    /// <summary>Resposta da avaliação de regras do contrato.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        string Protocol,
        int TotalViolations,
        int ErrorCount,
        int WarningCount,
        int InfoCount,
        IReadOnlyList<ViolationResponse> Violations);
}
