using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.CreateGovernancePack;

/// <summary>
/// Feature: CreateGovernancePack — cria um novo governance pack na plataforma.
/// Retorna o ID do pack criado para referência imediata.
/// MVP stub para validação de fluxo.
/// </summary>
public static class CreateGovernancePack
{
    /// <summary>Comando para criar um novo governance pack.</summary>
    public sealed record Command(
        string Name,
        string DisplayName,
        string? Description,
        string Category) : ICommand<Response>;

    /// <summary>Handler que cria um novo governance pack e retorna o ID gerado.</summary>
    public sealed class Handler : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var response = new Response(PackId: Guid.NewGuid().ToString());

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com o ID do governance pack criado.</summary>
    public sealed record Response(string PackId);
}
