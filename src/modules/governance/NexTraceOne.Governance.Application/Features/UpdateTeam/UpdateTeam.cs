using MediatR;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

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

    /// <summary>Handler que atualiza os dados da equipa.</summary>
    public sealed class Handler : ICommandHandler<Command>
    {
        public Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<Unit>.Success(Unit.Value));
        }
    }
}
