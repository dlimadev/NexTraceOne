using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.RulesetGovernance.Domain.Entities;

namespace NexTraceOne.RulesetGovernance.Application.Features.ComputeRulesetScore;

/// <summary>
/// Feature: ComputeRulesetScore -- computa o score de conformidade a partir de findings.
/// Formula: 100 - (errors * 10) - (warnings * 5) - (infos * 1), clamped entre 0 e 100.
/// Estrutura VSA: Command + Validator + Handler + Response em um unico arquivo.
/// </summary>
public static class ComputeRulesetScore
{
    /// <summary>Comando de computacao de score a partir de findings.</summary>
    public sealed record Command(IReadOnlyList<FindingInput> Findings) : ICommand<Response>;

    /// <summary>Dados de entrada de um finding para computacao de score.</summary>
    public sealed record FindingInput(FindingSeverity Severity);

    /// <summary>Valida a entrada do comando de computacao de score.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Findings).NotNull();
        }
    }

    /// <summary>Handler que computa o score de conformidade a partir dos findings.</summary>
    public sealed class Handler : ICommandHandler<Command, Response>
    {
        /// <summary>Processa o comando de computacao de score.</summary>
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var errors = request.Findings.Count(f => f.Severity == FindingSeverity.Error);
            var warnings = request.Findings.Count(f => f.Severity == FindingSeverity.Warning);
            var infos = request.Findings.Count(f => f.Severity == FindingSeverity.Info);

            var score = 100m - (errors * 10m) - (warnings * 5m) - (infos * 1m);
            score = Math.Clamp(score, 0m, 100m);

            Result<Response> result = new Response(score, errors, warnings, infos);
            return Task.FromResult(result);
        }
    }

    /// <summary>Resposta da computacao de score de conformidade.</summary>
    public sealed record Response(decimal Score, int ErrorCount, int WarningCount, int InfoCount);
}
