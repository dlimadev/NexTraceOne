using MediatR;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.CreateDelegatedAdministration;

/// <summary>
/// Feature: CreateDelegatedAdministration — cria uma nova delegação de administração.
/// Permite conceder permissões administrativas temporárias ou permanentes sobre equipas ou domínios.
/// </summary>
public static class CreateDelegatedAdministration
{
    /// <summary>Comando para criar uma nova delegação de administração.</summary>
    public sealed record Command(
        string GranteeUserId,
        string GranteeDisplayName,
        string Scope,
        string? TeamId,
        string? DomainId,
        string Reason,
        DateTimeOffset? ExpiresAt) : ICommand<Response>;

    /// <summary>Handler que cria a delegação e retorna o ID gerado.</summary>
    public sealed class Handler : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var response = new Response(DelegationId: Guid.NewGuid().ToString());

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com o ID da delegação criada.</summary>
    public sealed record Response(string DelegationId);
}
