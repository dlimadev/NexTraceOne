using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.ApproveGovernanceWaiver;

/// <summary>
/// Feature: ApproveGovernanceWaiver — aprova um pedido de exceção (waiver) de governança.
/// MVP stub para validação de fluxo.
/// </summary>
public static class ApproveGovernanceWaiver
{
    /// <summary>Comando para aprovar um waiver de governança.</summary>
    public sealed record Command(
        string WaiverId,
        string ReviewedBy) : ICommand<Response>;

    /// <summary>Handler que aprova o waiver e retorna o ID confirmado.</summary>
    public sealed class Handler : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var response = new Response(WaiverId: request.WaiverId);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com o ID do waiver aprovado.</summary>
    public sealed record Response(string WaiverId);
}
