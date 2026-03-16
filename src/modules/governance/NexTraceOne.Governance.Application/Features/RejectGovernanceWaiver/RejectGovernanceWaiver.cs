using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.RejectGovernanceWaiver;

/// <summary>
/// Feature: RejectGovernanceWaiver — rejeita um pedido de exceção (waiver) de governança.
/// MVP stub para validação de fluxo.
/// </summary>
public static class RejectGovernanceWaiver
{
    /// <summary>Comando para rejeitar um waiver de governança.</summary>
    public sealed record Command(
        string WaiverId,
        string ReviewedBy) : ICommand<Response>;

    /// <summary>Handler que rejeita o waiver e retorna o ID confirmado.</summary>
    public sealed class Handler : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var response = new Response(WaiverId: request.WaiverId);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com o ID do waiver rejeitado.</summary>
    public sealed record Response(string WaiverId);
}
