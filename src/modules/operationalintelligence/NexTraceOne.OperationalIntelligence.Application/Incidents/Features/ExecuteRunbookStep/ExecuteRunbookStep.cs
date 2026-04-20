using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ExecuteRunbookStep;

/// <summary>
/// Feature: ExecuteRunbookStep — regista a execução de um passo de runbook operacional.
///
/// Valida que o runbook existe, cria a entidade RunbookStepExecution, marca-a como
/// bem-sucedida (execução simulada) e persiste o registo via IRunbookExecutionRepository.
/// Permite rastreabilidade completa de quem executou cada passo e quando.
///
/// Estrutura VSA: Command + Validator + Handler + Response em ficheiro único.
/// </summary>
public static class ExecuteRunbookStep
{
    private const string SimulatedOutputSummary = "Step executed successfully (simulated execution).";

    /// <summary>Comando para registar a execução de um passo de runbook.</summary>
    public sealed record Command(
        Guid RunbookId,
        string StepKey,
        string ExecutorUserId) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RunbookId).NotEmpty();
            RuleFor(x => x.StepKey).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ExecutorUserId).NotEmpty().MaximumLength(500);
        }
    }

    /// <summary>Handler que persiste a execução do passo de runbook.</summary>
    public sealed class Handler(
        IRunbookRepository runbookRepository,
        IRunbookExecutionRepository executionRepository,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var runbook = await runbookRepository.GetByIdAsync(request.RunbookId, cancellationToken);
            if (runbook is null)
                return IncidentErrors.RunbookNotFound(request.RunbookId.ToString());

            var tenantId = currentTenant.IsActive ? currentTenant.Id : (Guid?)null;
            var startedAt = clock.UtcNow;

            var execution = RunbookStepExecution.Create(
                request.RunbookId,
                request.StepKey,
                request.ExecutorUserId,
                startedAt,
                tenantId);

            var completedAt = clock.UtcNow;
            execution.MarkSucceeded(SimulatedOutputSummary, completedAt);

            await executionRepository.AddAsync(execution, cancellationToken);

            return Result<Response>.Success(new Response(
                execution.Id.Value,
                execution.RunbookId,
                execution.StepKey,
                execution.ExecutionStatus.ToString(),
                execution.StartedAt,
                execution.CompletedAt,
                execution.OutputSummary));
        }
    }

    /// <summary>Resposta com os dados da execução registada.</summary>
    public sealed record Response(
        Guid ExecutionId,
        Guid RunbookId,
        string StepKey,
        string Status,
        DateTimeOffset StartedAt,
        DateTimeOffset? CompletedAt,
        string? OutputSummary);
}
