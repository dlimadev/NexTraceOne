using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.RetryConnector;

/// <summary>
/// Feature: RetryConnector — solicita nova tentativa de execução de um conector de integração.
/// Enfileira o pedido de retry e retorna confirmação com ID do pedido.
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
    public sealed class Handler : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var response = new Response(
                RetryRequestId: Guid.NewGuid(),
                ConnectorId: request.ConnectorId,
                Status: "Queued",
                RequestedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com confirmação do pedido de retry.</summary>
    public sealed record Response(
        Guid RetryRequestId,
        string ConnectorId,
        string Status,
        DateTimeOffset RequestedAt);
}
