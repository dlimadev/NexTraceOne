using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.CreateGovernanceWaiver;

/// <summary>
/// Feature: CreateGovernanceWaiver — cria um pedido de exceção (waiver) para uma regra de governança.
/// Retorna o ID do waiver criado para acompanhamento do fluxo de aprovação.
/// MVP stub para validação de fluxo.
/// </summary>
public static class CreateGovernanceWaiver
{
    /// <summary>Comando para criar um novo waiver de governança.</summary>
    public sealed record Command(
        string PackId,
        string? RuleId,
        string Scope,
        string ScopeType,
        string Justification,
        string RequestedBy,
        DateTimeOffset? ExpiresAt,
        IReadOnlyList<string> EvidenceLinks) : ICommand<Response>;

    /// <summary>Handler que cria o waiver e retorna o ID gerado.</summary>
    public sealed class Handler : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var response = new Response(WaiverId: Guid.NewGuid().ToString());

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com o ID do waiver criado.</summary>
    public sealed record Response(string WaiverId);
}
