using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

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
    public sealed class Handler(
        IDelegatedAdministrationRepository delegationRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Parse do Scope
            if (!Enum.TryParse<DelegationScope>(request.Scope, ignoreCase: true, out var scope))
                return Error.Validation("INVALID_SCOPE", "Scope '{0}' is not valid.", request.Scope);

            var delegation = DelegatedAdministration.Create(
                granteeUserId: request.GranteeUserId,
                granteeDisplayName: request.GranteeDisplayName,
                scope: scope,
                teamId: request.TeamId,
                domainId: request.DomainId,
                reason: request.Reason,
                expiresAt: request.ExpiresAt);

            await delegationRepository.AddAsync(delegation, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new Response(DelegationId: delegation.Id.Value.ToString());

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta com o ID da delegação criada.</summary>
    public sealed record Response(string DelegationId);
}
