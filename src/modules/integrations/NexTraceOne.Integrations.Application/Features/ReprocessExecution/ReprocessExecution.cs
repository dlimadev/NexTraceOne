using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;
using ProcessIngestionPayloadFeature = NexTraceOne.Integrations.Application.Features.ProcessIngestionPayload.ProcessIngestionPayload;

namespace NexTraceOne.Integrations.Application.Features.ReprocessExecution;

/// <summary>
/// Feature: ReprocessExecution — solicita reprocessamento de uma execução de ingestão.
/// Enfileira o pedido de reprocessamento e retorna confirmação com ID do pedido.
/// Handler nativo do módulo Integrations.
/// Ownership: módulo Integrations.
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
        IDateTimeProvider clock,
        ISender sender) : ICommandHandler<Command, Response>
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

            // Attempt semantic processing of the new execution.
            // RawPayload is not available on the original execution yet (pending migration),
            // so processing falls back to metadata_recorded gracefully.
            var processResult = await sender.Send(
                new ProcessIngestionPayloadFeature.Command(newExecution.Id.Value, RawPayload: null),
                cancellationToken);

            var processingStatus = processResult.IsSuccess ? processResult.Value.Status : "metadata_recorded";

            var response = new Response(
                ReprocessRequestId: newExecution.Id.Value,
                ExecutionId: request.ExecutionId,
                Status: "Queued",
                ProcessingStatus: processingStatus,
                RequestedAt: clock.UtcNow);

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta com confirmação do pedido de reprocessamento.</summary>
    public sealed record Response(
        Guid ReprocessRequestId,
        string ExecutionId,
        string Status,
        string ProcessingStatus,
        DateTimeOffset RequestedAt);
}
