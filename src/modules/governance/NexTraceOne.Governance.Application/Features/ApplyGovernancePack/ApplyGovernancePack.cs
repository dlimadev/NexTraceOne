using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.ApplyGovernancePack;

/// <summary>
/// Feature: ApplyGovernancePack — aplica um governance pack a um escopo específico.
/// Inicia o rollout do pack com enforcement mode definido.
/// MVP stub para validação de fluxo.
/// </summary>
public static class ApplyGovernancePack
{
    /// <summary>Comando para aplicar um governance pack a um escopo.</summary>
    public sealed record Command(
        string PackId,
        string ScopeType,
        string ScopeValue,
        string EnforcementMode,
        string AppliedBy) : ICommand<Response>;

    /// <summary>Handler que aplica o governance pack e retorna o ID do rollout.</summary>
    public sealed class Handler : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var response = new Response(RolloutId: Guid.NewGuid().ToString());

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com o ID do rollout iniciado.</summary>
    public sealed record Response(string RolloutId);
}
