using Ardalis.GuardClauses;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AuditCompliance.Application.Features.GetRetentionPolicies;

/// <summary>
/// Feature: GetRetentionPolicies — lista as políticas de retenção de eventos de auditoria.
/// P7.4 — expõe as políticas persistidas para que operadores e auditores possam consultar
/// as configurações de retenção activas e históricas.
/// </summary>
public static class GetRetentionPolicies
{
    /// <summary>Query para listar políticas de retenção.</summary>
    public sealed record Query(bool? ActiveOnly = null) : IQuery<Response>;

    /// <summary>DTO de uma política de retenção.</summary>
    public sealed record RetentionPolicyDto(
        Guid PolicyId,
        string PolicyName,
        int RetentionDays,
        bool IsActive);

    /// <summary>Resposta com a lista de políticas de retenção.</summary>
    public sealed record Response(IReadOnlyList<RetentionPolicyDto> Policies);

    public sealed class Handler(
        IRetentionPolicyRepository retentionPolicyRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var policies = request.ActiveOnly is true
                ? await retentionPolicyRepository.ListActiveAsync(cancellationToken)
                : await retentionPolicyRepository.ListAllAsync(cancellationToken);

            var dtos = policies
                .Select(p => new RetentionPolicyDto(p.Id.Value, p.Name, p.RetentionDays, p.IsActive))
                .ToList();

            return new Response(dtos);
        }
    }
}
