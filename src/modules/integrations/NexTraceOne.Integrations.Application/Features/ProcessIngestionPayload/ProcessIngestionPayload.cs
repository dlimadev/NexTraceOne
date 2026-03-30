using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Application.Features.ProcessIngestionPayload;

/// <summary>
/// Feature: ProcessIngestionPayload — parsing semântico de payloads de deploy/change.
/// Extrai campos conhecidos do JSON, enriquece a execução e publica evento de domínio.
/// Ownership: módulo Integrations.
/// </summary>
public static class ProcessIngestionPayload
{
    /// <summary>Comando para processar o payload de uma execução de ingestão.</summary>
    public sealed record Command(Guid ExecutionId, string? RawPayload) : ICommand<Response>;

    /// <summary>
    /// Handler que carrega a execução, invoca o parser, persiste os campos extraídos
    /// e emite <see cref="IngestionPayloadProcessedDomainEvent"/> via outbox em caso de sucesso.
    /// O domain event é emitido automaticamente pelo aggregate root durante SaveChanges,
    /// persistido na tabela outbox e processado assincronamente pelo ModuleOutboxProcessorJob.
    /// Degradação graciosa: qualquer falha de parsing mantém o status metadata_recorded
    /// e nunca propaga excepção para o caller.
    /// </summary>
    public sealed class Handler(
        IIngestionExecutionRepository executionRepository,
        IIngestionPayloadParser payloadParser,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var executionId = new IngestionExecutionId(request.ExecutionId);
            var execution = await executionRepository.GetByIdAsync(executionId, cancellationToken);

            if (execution is null)
            {
                return Error.NotFound("EXECUTION_NOT_FOUND", $"Execution {request.ExecutionId} not found");
            }

            if (string.IsNullOrWhiteSpace(request.RawPayload))
            {
                execution.MarkAsFailed("Raw payload is empty — cannot perform semantic processing");
                await executionRepository.UpdateAsync(execution, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);
                return Result<Response>.Success(new Response(request.ExecutionId, "metadata_recorded"));
            }

            try
            {
                var parsed = payloadParser.ParseDeployPayload(request.RawPayload);

                if (!parsed.IsSuccessful)
                {
                    logger.LogWarning(
                        "Payload parsing failed for execution {ExecutionId}: {Error}",
                        request.ExecutionId, parsed.ErrorMessage);

                    execution.MarkAsFailed(parsed.ErrorMessage ?? "Parsing failed");
                    await executionRepository.UpdateAsync(execution, cancellationToken);
                    await unitOfWork.CommitAsync(cancellationToken);
                    return Result<Response>.Success(new Response(request.ExecutionId, "metadata_recorded"));
                }

                var parsedAt = clock.UtcNow;
                execution.MarkAsProcessed(
                    serviceName: parsed.ServiceName,
                    environment: parsed.Environment,
                    version: parsed.Version,
                    commitSha: parsed.CommitSha,
                    changeType: parsed.ChangeType,
                    parsedAt: parsedAt);

                logger.LogInformation(
                    "Execution {ExecutionId} payload processed: service={Service} env={Environment} version={Version}",
                    request.ExecutionId, parsed.ServiceName, parsed.Environment, parsed.Version);

                await executionRepository.UpdateAsync(execution, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);
                // Domain event IngestionPayloadProcessedDomainEvent is raised by the aggregate root
                // in MarkAsProcessed() and captured by the outbox during SaveChanges/CommitAsync.

                return Result<Response>.Success(new Response(request.ExecutionId, "processed"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Unexpected error processing payload for execution {ExecutionId} — falling back to metadata_recorded",
                    request.ExecutionId);

                try
                {
                    execution.MarkAsFailed($"Unexpected error: {ex.GetType().Name}");
                    await executionRepository.UpdateAsync(execution, cancellationToken);
                    await unitOfWork.CommitAsync(cancellationToken);
                }
                catch (Exception persistEx)
                {
                    logger.LogError(persistEx,
                        "Failed to persist fallback status for execution {ExecutionId}",
                        request.ExecutionId);
                }

                return Result<Response>.Success(new Response(request.ExecutionId, "metadata_recorded"));
            }
        }
    }

    /// <summary>Resposta com o ID da execução e o status de processamento resultante.</summary>
    public sealed record Response(Guid ExecutionId, string Status);
}
