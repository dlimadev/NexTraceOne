using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

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
    public sealed class Handler(
        ITeamRepository teamRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Verifica se já existe equipa com o mesmo nome
            var existing = await teamRepository.GetByNameAsync(request.Name, cancellationToken);
            if (existing is not null)
                return Error.Conflict("TEAM_NAME_EXISTS", "Team with name '{0}' already exists.", request.Name);

            var team = Team.Create(
                name: request.Name,
                displayName: request.DisplayName,
                description: request.Description,
                parentOrganizationUnit: request.ParentOrganizationUnit);

            await teamRepository.AddAsync(team, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new Response(TeamId: team.Id.Value.ToString());

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta com o ID da equipa criada.</summary>
    public sealed record Response(string TeamId);
}
