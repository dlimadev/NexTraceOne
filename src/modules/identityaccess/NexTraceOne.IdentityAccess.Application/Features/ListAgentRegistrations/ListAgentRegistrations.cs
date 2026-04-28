using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;

namespace NexTraceOne.IdentityAccess.Application.Features.ListAgentRegistrations;

/// <summary>
/// SaaS-03: Lista os agentes registados do tenant corrente.
/// </summary>
public static class ListAgentRegistrations
{
    public sealed record Query : IQuery<Response>;

    public sealed record AgentRegistrationDto(
        Guid RegistrationId,
        Guid HostUnitId,
        string HostName,
        string AgentVersion,
        string DeploymentMode,
        int CpuCores,
        decimal RamGb,
        decimal HostUnits,
        string Status,
        DateTimeOffset LastHeartbeatAt,
        DateTimeOffset RegisteredAt);

    public sealed record Response(
        IReadOnlyList<AgentRegistrationDto> Agents,
        decimal TotalHostUnits,
        int ActiveCount);

    public sealed class Handler(
        IAgentRegistrationRepository repository,
        ICurrentTenant currentTenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            var tenantId = currentTenant.TenantId;

            var agents = await repository.ListByTenantAsync(tenantId, cancellationToken);

            var dtos = agents.Select(a => new AgentRegistrationDto(
                a.Id.Value,
                a.HostUnitId,
                a.HostName,
                a.AgentVersion,
                a.DeploymentMode,
                a.CpuCores,
                a.RamGb,
                a.HostUnits,
                a.Status.ToString(),
                a.LastHeartbeatAt,
                a.RegisteredAt)).ToList();

            var totalHostUnits = agents.Where(a => a.Status == Domain.Entities.AgentRegistrationStatus.Active)
                .Sum(a => a.HostUnits);
            var activeCount = agents.Count(a => a.Status == Domain.Entities.AgentRegistrationStatus.Active);

            return Result.Success(new Response(dtos, totalHostUnits, activeCount));
        }
    }
}
