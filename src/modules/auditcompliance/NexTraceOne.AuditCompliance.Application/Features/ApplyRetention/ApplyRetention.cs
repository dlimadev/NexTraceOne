using Ardalis.GuardClauses;

using MediatR;

using Microsoft.Extensions.Logging;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AuditCompliance.Application.Features.ApplyRetention;

/// <summary>
/// Feature: ApplyRetention — aplica políticas de retenção activas eliminando eventos expirados.
/// P7.4 — torna RetentionPolicy funcionalmente real: a aplicação efectiva deleta AuditEvents
/// anteriores ao cutoff definido pela política mais restritiva activa.
///
/// Estratégia:
///   1. Obter a política activa mais restritiva (menor RetentionDays).
///   2. Calcular cutoff = UtcNow - RetentionDays.
///   3. Eliminar AuditEvents com OccurredAt &lt; cutoff via ExecuteDeleteAsync (bulk delete).
///   4. Retornar o número de eventos eliminados e os detalhes da política aplicada.
///
/// Limitações actuais (P7.4):
///   - Aplica apenas a política global mais restritiva (não filtra por módulo ou tipo).
///   - AuditChainLinks dos eventos eliminados ficam órfãos (hash chain fica parcialmente truncada).
///     Resolução completa fica para P7.5.
/// </summary>
public static class ApplyRetention
{
    /// <summary>Comando que desencadeia a aplicação de retenção.</summary>
    public sealed record Command : ICommand<Response>;

    /// <summary>Resposta com o resultado da aplicação de retenção.</summary>
    public sealed record Response(
        bool PolicyApplied,
        string? PolicyName,
        int RetentionDays,
        DateTimeOffset Cutoff,
        int DeletedEventCount);

    public sealed class Handler(
        IRetentionPolicyRepository retentionPolicyRepository,
        IAuditEventRepository auditEventRepository,
        IDateTimeProvider clock,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // 1. Obter a política activa mais restritiva
            var policy = await retentionPolicyRepository.GetMostRestrictiveActiveAsync(cancellationToken);

            if (policy is null)
            {
                logger.LogInformation(
                    "ApplyRetention: no active retention policy found. No events deleted.");
                return new Response(
                    PolicyApplied: false,
                    PolicyName: null,
                    RetentionDays: 0,
                    Cutoff: clock.UtcNow,
                    DeletedEventCount: 0);
            }

            // 2. Calcular cutoff
            var cutoff = clock.UtcNow.AddDays(-policy.RetentionDays);

            // 3. Eliminar eventos expirados
            var deletedCount = await auditEventRepository.DeleteExpiredAsync(cutoff, cancellationToken);

            logger.LogInformation(
                "ApplyRetention: policy={PolicyName} retentionDays={RetentionDays} cutoff={Cutoff} deleted={Count}",
                policy.Name, policy.RetentionDays, cutoff, deletedCount);

            return new Response(
                PolicyApplied: true,
                PolicyName: policy.Name,
                RetentionDays: policy.RetentionDays,
                Cutoff: cutoff,
                DeletedEventCount: deletedCount);
        }
    }
}
