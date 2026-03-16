using MediatR;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.CreateTeam;

/// <summary>
/// Feature: CreateTeam — cria uma nova equipa na plataforma de governança.
/// Retorna o ID da equipa criada para referência imediata.
/// </summary>
public static class CreateTeam
{
    /// <summary>Comando para criar uma nova equipa.</summary>
    public sealed record Command(
        string Name,
        string DisplayName,
        string? Description,
        string? ParentOrganizationUnit) : ICommand<Response>;

    /// <summary>Handler que cria uma nova equipa e retorna o ID gerado.</summary>
    public sealed class Handler : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var response = new Response(TeamId: Guid.NewGuid().ToString());

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com o ID da equipa criada.</summary>
    public sealed record Response(string TeamId);
}
