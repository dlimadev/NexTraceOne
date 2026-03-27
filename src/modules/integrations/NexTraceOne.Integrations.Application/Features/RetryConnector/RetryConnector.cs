using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Application.Features.RetryConnector;

/// <summary>
/// Feature: RetryConnector — solicita nova tentativa de execução de um conector de integração.
/// Enfileira o pedido de retry e retorna confirmação com ID do pedido.
/// Handler nativo do módulo Integrations.
/// Ownership: módulo Integrations.
/// </summary>
public static class RetryConnector
{
    /// <summary>Comando para solicitar retry de um conector.</summary>
    public sealed record Command(string ConnectorId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de retry.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ConnectorId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que enfileira o pedido de retry do conector.</summary>
    public sealed class Handler(
        IIntegrationConnectorRepository connectorRepository,
        IIngestionExecutionRepository executionRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.ConnectorId, out var connectorGuid))
            {
                return Error.Validation("INVALID_CONNECTOR_ID", "Invalid connector ID format");
            }

            var connectorId = new IntegrationConnectorId(connectorGuid);
            var connector = await connectorRepository.GetByIdAsync(connectorId, cancellationToken);

            if (connector is null)
            {
                return Error.NotFound("CONNECTOR_NOT_FOUND", $"Connector {request.ConnectorId} not found");
            }

            // Create a new execution for the retry
            var execution = IngestionExecution.Start(
                connectorId: connectorId,
                sourceId: null,
                correlationId: $"retry-{Guid.NewGuid():N}",
                utcNow: clock.UtcNow);

            await executionRepository.AddAsync(execution, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new Response(
                RetryRequestId: execution.Id.Value,
                ConnectorId: request.ConnectorId,
                Status: "Queued",
                RequestedAt: clock.UtcNow);

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta com confirmação do pedido de retry.</summary>
    public sealed record Response(
        Guid RetryRequestId,
        string ConnectorId,
        string Status,
        DateTimeOffset RequestedAt);
}
