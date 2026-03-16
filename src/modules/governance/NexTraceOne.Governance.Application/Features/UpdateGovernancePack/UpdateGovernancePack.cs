using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.UpdateGovernancePack;

/// <summary>
/// Feature: UpdateGovernancePack — atualiza propriedades de um governance pack existente.
/// MVP stub para validação de fluxo.
/// </summary>
public static class UpdateGovernancePack
{
    /// <summary>Comando para atualizar um governance pack existente.</summary>
    public sealed record Command(
        string PackId,
        string? DisplayName,
        string? Description,
        string? Category) : ICommand<Response>;

    /// <summary>Handler que atualiza o governance pack e retorna o ID confirmado.</summary>
    public sealed class Handler : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var response = new Response(PackId: request.PackId);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com o ID do governance pack atualizado.</summary>
    public sealed record Response(string PackId);
}
