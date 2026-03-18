using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.ReprocessExecution;

/// <summary>
/// Feature: ReprocessExecution — solicita reprocessamento de uma execução de ingestão.
/// Enfileira o pedido de reprocessamento e retorna confirmação com ID do pedido.
/// </summary>
public static class ReprocessExecution
{
    /// <summary>Comando para solicitar reprocessamento de uma execução.</summary>
    public sealed record Command(string ExecutionId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de reprocessamento.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ExecutionId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que enfileira o pedido de reprocessamento da execução.</summary>
    public sealed class Handler(
        IIngestionExecutionRepository executionRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.ExecutionId, out var executionGuid))
            {
                return Error.Validation("INVALID_EXECUTION_ID", "Invalid execution ID format");
            }

            var executionId = new IngestionExecutionId(executionGuid);
            var originalExecution = await executionRepository.GetByIdAsync(executionId, cancellationToken);

            if (originalExecution is null)
            {
                return Error.NotFound("EXECUTION_NOT_FOUND", $"Execution {request.ExecutionId} not found");
            }

            // Create a new execution for reprocessing
            var newExecution = IngestionExecution.Start(
                connectorId: originalExecution.ConnectorId,
                sourceId: originalExecution.SourceId,
                correlationId: $"reprocess-{originalExecution.Id.Value:N}",
                utcNow: clock.UtcNow);

            await executionRepository.AddAsync(newExecution, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new Response(
                ReprocessRequestId: newExecution.Id.Value,
                ExecutionId: request.ExecutionId,
                Status: "Queued",
                RequestedAt: clock.UtcNow);

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta com confirmação do pedido de reprocessamento.</summary>
    public sealed record Response(
        Guid ReprocessRequestId,
        string ExecutionId,
        string Status,
        DateTimeOffset RequestedAt);
}
