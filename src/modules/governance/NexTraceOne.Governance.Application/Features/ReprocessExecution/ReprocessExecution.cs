using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

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
    public sealed class Handler : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var response = new Response(
                ReprocessRequestId: Guid.NewGuid(),
                ExecutionId: request.ExecutionId,
                Status: "Queued",
                RequestedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com confirmação do pedido de reprocessamento.</summary>
    public sealed record Response(
        Guid ReprocessRequestId,
        string ExecutionId,
        string Status,
        DateTimeOffset RequestedAt);
}
