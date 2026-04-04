using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.UpdateTeam;

/// <summary>
/// Feature: UpdateTeam — atualiza os dados de uma equipa existente.
/// Permite alteração do nome de exibição, descrição e unidade organizacional.
/// </summary>
public static class UpdateTeam
{
    /// <summary>Comando para atualizar uma equipa existente.</summary>
    public sealed record Command(
        string TeamId,
        string DisplayName,
        string? Description,
        string? ParentOrganizationUnit) : ICommand;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TeamId).NotEmpty().MaximumLength(50);
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000)
                .When(x => x.Description is not null);
            RuleFor(x => x.ParentOrganizationUnit).MaximumLength(200)
                .When(x => x.ParentOrganizationUnit is not null);
        }
    }

    /// <summary>Handler que atualiza os dados da equipa.</summary>
    public sealed class Handler(
        ITeamRepository teamRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.TeamId, out var teamGuid))
                return Error.Validation("INVALID_TEAM_ID", "Team ID '{0}' is not a valid GUID.", request.TeamId);

            var team = await teamRepository.GetByIdAsync(new TeamId(teamGuid), cancellationToken);
            if (team is null)
                return Error.NotFound("TEAM_NOT_FOUND", "Team '{0}' not found.", request.TeamId);

            team.Update(
                displayName: request.DisplayName,
                description: request.Description,
                parentOrganizationUnit: request.ParentOrganizationUnit);

            await teamRepository.UpdateAsync(team, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
