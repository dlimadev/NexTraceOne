using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.DeleteUserTokenQuota;

/// <summary>
/// Feature: DeleteUserTokenQuota — desativa a quota de tokens de um utilizador.
/// </summary>
public static class DeleteUserTokenQuota
{
    /// <summary>Comando de desativação de quota de tokens.</summary>
    public sealed record Command(Guid QuotaId) : ICommand<Response>;

    /// <summary>Handler que desativa a quota de tokens.</summary>
    public sealed class Handler(
        IAiTokenQuotaPolicyRepository quotaRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var quota = await quotaRepository.GetByIdAsync(
                AiTokenQuotaPolicyId.From(request.QuotaId), cancellationToken);

            if (quota is null)
                return Error.NotFound(
                    "UserTokenQuota.NotFound",
                    "Quota '{0}' não encontrada.",
                    request.QuotaId);

            quota.Disable();

            return new Response(request.QuotaId);
        }
    }

    /// <summary>Resposta da desativação da quota.</summary>
    public sealed record Response(Guid QuotaId);
}
