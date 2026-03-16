using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.CreatePackVersion;

/// <summary>
/// Feature: CreatePackVersion — cria uma nova versão de um governance pack.
/// Retorna o ID da versão criada para referência imediata.
/// MVP stub para validação de fluxo.
/// </summary>
public static class CreatePackVersion
{
    /// <summary>Comando para criar uma nova versão de governance pack.</summary>
    public sealed record Command(
        string PackId,
        string Version,
        string DefaultEnforcementMode,
        string? ChangeDescription,
        string CreatedBy) : ICommand<Response>;

    /// <summary>Handler que cria uma nova versão e retorna o ID gerado.</summary>
    public sealed class Handler : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var response = new Response(VersionId: Guid.NewGuid().ToString());

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com o ID da versão criada.</summary>
    public sealed record Response(string VersionId);
}
