using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.ExecutePlaybook;

/// <summary>
/// Feature: ExecutePlaybook — inicia a execução de um playbook operacional ativo.
/// Cria um registo de PlaybookExecution no estado InProgress e incrementa o contador no playbook.
///
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ExecutePlaybook
{
    /// <summary>Comando para iniciar a execução de um playbook.</summary>
    public sealed record Command(
        Guid PlaybookId,
        Guid? IncidentId) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando de execução.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PlaybookId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que cria uma execução de playbook e incrementa o contador de execuções.
    /// </summary>
    public sealed class Handler(
        IOperationalPlaybookRepository playbookRepository,
        IPlaybookExecutionRepository executionRepository,
        IUnitOfWork unitOfWork,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var playbook = await playbookRepository.GetByIdAsync(
                OperationalPlaybookId.From(request.PlaybookId),
                cancellationToken);

            if (playbook is null)
                return RuntimeIntelligenceErrors.PlaybookNotFound(request.PlaybookId.ToString());

            if (playbook.Status != PlaybookStatus.Active)
                return RuntimeIntelligenceErrors.PlaybookNotActive(request.PlaybookId.ToString());

            var now = dateTimeProvider.UtcNow;

            var execution = PlaybookExecution.Start(
                playbookId: playbook.Id.Value,
                playbookName: playbook.Name,
                incidentId: request.IncidentId,
                executedByUserId: currentUser.Id,
                tenantId: currentTenant.Id.ToString(),
                startedAt: now);

            playbook.IncrementExecutionCount(now);

            await executionRepository.AddAsync(execution, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                execution.Id.Value,
                playbook.Id.Value,
                playbook.Name,
                execution.Status.ToString(),
                now));
        }
    }

    /// <summary>Resposta com os dados da execução iniciada.</summary>
    public sealed record Response(
        Guid ExecutionId,
        Guid PlaybookId,
        string PlaybookName,
        string Status,
        DateTimeOffset StartedAt);
}
