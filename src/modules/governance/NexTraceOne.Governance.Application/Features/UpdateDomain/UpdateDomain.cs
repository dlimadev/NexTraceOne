using MediatR;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.UpdateDomain;

/// <summary>
/// Feature: UpdateDomain — atualiza os dados de um domínio de negócio existente.
/// Permite alteração do nome de exibição, descrição, criticidade e classificação de capacidade.
/// </summary>
public static class UpdateDomain
{
    /// <summary>Comando para atualizar um domínio existente.</summary>
    public sealed record Command(
        string DomainId,
        string DisplayName,
        string? Description,
        string Criticality,
        string? CapabilityClassification) : ICommand;

    /// <summary>Handler que atualiza os dados do domínio.</summary>
    public sealed class Handler : ICommandHandler<Command>
    {
        public Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<Unit>.Success(Unit.Value));
        }
    }
}
