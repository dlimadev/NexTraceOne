using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Automation.Features.EvaluatePreconditions;

/// <summary>
/// Feature: EvaluatePreconditions — avalia as pré-condições de um workflow de automação
/// para determinar se todas as condições obrigatórias estão satisfeitas para execução segura.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
///
/// Nota: nesta fase os dados são simulados até integração completa entre módulos.
/// </summary>
public static class EvaluatePreconditions
{
    /// <summary>Comando para avaliar as pré-condições de um workflow.</summary>
    public sealed record Command(
        string WorkflowId,
        string EvaluatedBy) : ICommand<Response>;

    /// <summary>Valida os campos obrigatórios do comando de avaliação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.WorkflowId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.EvaluatedBy).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que avalia as pré-condições com dados simulados.</summary>
    public sealed class Handler : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;

            var results = new List<PreconditionResult>
            {
                new(PreconditionType.ServiceHealthCheck, true,
                    "Service health check passed — all instances reporting healthy.",
                    now.AddSeconds(-10)),

                new(PreconditionType.ApprovalPresence, true,
                    "Approval from authorized persona is present.",
                    now.AddSeconds(-8)),

                new(PreconditionType.BlastRadiusConstraint, true,
                    "Blast radius limited to single pod group — within acceptable limits.",
                    now.AddSeconds(-6)),

                new(PreconditionType.EnvironmentRestriction, true,
                    "Target environment 'Production' is within allowed environments for this action.",
                    now.AddSeconds(-4)),

                new(PreconditionType.CooldownPeriod, true,
                    "No recent executions of this action type — cooldown period satisfied.",
                    now.AddSeconds(-2)),
            };

            var allPassed = results.All(r => r.Passed);

            var response = new Response(
                WorkflowId: Guid.TryParse(request.WorkflowId, out var wfId) ? wfId : Guid.NewGuid(),
                AllPassed: allPassed,
                Results: results);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resultado da avaliação de uma pré-condição individual.</summary>
    public sealed record PreconditionResult(
        PreconditionType Type,
        bool Passed,
        string Details,
        DateTimeOffset EvaluatedAt);

    /// <summary>Resposta da avaliação de pré-condições do workflow.</summary>
    public sealed record Response(
        Guid WorkflowId,
        bool AllPassed,
        IReadOnlyList<PreconditionResult> Results);
}
