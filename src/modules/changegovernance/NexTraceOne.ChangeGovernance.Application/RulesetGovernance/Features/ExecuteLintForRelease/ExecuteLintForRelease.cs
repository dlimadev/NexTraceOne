using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.RulesetGovernance.Application.Abstractions;
using NexTraceOne.RulesetGovernance.Domain.Entities;
using NexTraceOne.RulesetGovernance.Domain.Errors;

namespace NexTraceOne.RulesetGovernance.Application.Features.ExecuteLintForRelease;

/// <summary>
/// Feature: ExecuteLintForRelease -- registra o resultado de uma execução de linting sobre uma release.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ExecuteLintForRelease
{
    /// <summary>Comando de registro de resultado de linting.</summary>
    public sealed record Command(
        Guid RulesetId,
        Guid ReleaseId,
        Guid ApiAssetId,
        IReadOnlyList<FindingInput> Findings) : ICommand<Response>;

    /// <summary>Dados de entrada de um finding individual.</summary>
    public sealed record FindingInput(string Rule, FindingSeverity Severity, string Message, string Path);

    /// <summary>Valida a entrada do comando de execução de linting.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RulesetId).NotEmpty();
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.Findings).NotNull();
        }
    }

    /// <summary>Handler que cria um LintResult a partir dos findings informados.</summary>
    public sealed class Handler(
        IRulesetRepository rulesetRepository,
        ILintResultRepository lintResultRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        /// <summary>Processa o comando de execução de linting.</summary>
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var rulesetId = RulesetId.From(request.RulesetId);
            var ruleset = await rulesetRepository.GetByIdAsync(rulesetId, cancellationToken);
            if (ruleset is null)
                return RulesetGovernanceErrors.RulesetNotFound(request.RulesetId.ToString());

            var findings = request.Findings
                .Select(f => Finding.Create(f.Rule, f.Severity, f.Message, f.Path))
                .ToList();

            var score = ComputeScore(findings);

            var lintResult = LintResult.Create(
                rulesetId,
                request.ReleaseId,
                request.ApiAssetId,
                score,
                findings,
                dateTimeProvider.UtcNow);

            lintResultRepository.Add(lintResult);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                lintResult.Id.Value,
                lintResult.ReleaseId,
                lintResult.Score,
                lintResult.TotalFindings,
                lintResult.ExecutedAt);
        }

        private static decimal ComputeScore(IReadOnlyList<Finding> findings)
        {
            var errors = findings.Count(f => f.Severity == FindingSeverity.Error);
            var warnings = findings.Count(f => f.Severity == FindingSeverity.Warning);
            var infos = findings.Count(f => f.Severity == FindingSeverity.Info);

            var score = 100m - (errors * 10m) - (warnings * 5m) - (infos * 1m);
            return Math.Clamp(score, 0m, 100m);
        }
    }

    /// <summary>Resposta da execução de linting.</summary>
    public sealed record Response(
        Guid LintResultId,
        Guid ReleaseId,
        decimal Score,
        int TotalFindings,
        DateTimeOffset ExecutedAt);
}
