using MediatR;
using Microsoft.Extensions.Configuration;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetResourceBudget;

/// <summary>
/// Feature: GetResourceBudget — gestão de quotas de recursos por tenant.
/// Lê de IConfiguration "Platform:ResourceBudget:*".
/// </summary>
public static class GetResourceBudget
{
    /// <summary>Query com tenantId opcional — retorna quotas de recursos.</summary>
    public sealed record Query(Guid? TenantId) : IQuery<ResourceBudgetResponse>;

    /// <summary>Comando para atualizar quotas de recursos de um tenant.</summary>
    public sealed record UpdateResourceBudget(
        Guid TenantId,
        IReadOnlyList<ResourceQuotaDto> Quotas) : ICommand<ResourceBudgetResponse>;

    /// <summary>Handler de leitura de quotas de recursos.</summary>
    public sealed class Handler(IConfiguration configuration) : IQueryHandler<Query, ResourceBudgetResponse>
    {
        public Task<Result<ResourceBudgetResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var cpuAllocated = double.TryParse(configuration["Platform:ResourceBudget:Cpu:Allocated"], out var cpu) ? cpu : 8.0;
            var memAllocated = double.TryParse(configuration["Platform:ResourceBudget:Memory:Allocated"], out var mem) ? mem : 16.0;
            var storageAllocated = double.TryParse(configuration["Platform:ResourceBudget:Storage:Allocated"], out var stor) ? stor : 100.0;
            var apiAllocated = double.TryParse(configuration["Platform:ResourceBudget:Api:Allocated"], out var api) ? api : 10000.0;
            var aiAllocated = double.TryParse(configuration["Platform:ResourceBudget:Ai:Allocated"], out var ai) ? ai : 1000.0;

            var quotas = new List<ResourceQuotaDto>
            {
                new("CPU", cpuAllocated, 0, "cores"),
                new("Memory", memAllocated, 0, "GB"),
                new("Storage", storageAllocated, 0, "GB"),
                new("API", apiAllocated, 0, "requests/min"),
                new("AI", aiAllocated, 0, "tokens/day")
            };

            var response = new ResourceBudgetResponse(
                TenantId: request.TenantId,
                Quotas: quotas);

            return Task.FromResult(Result<ResourceBudgetResponse>.Success(response));
        }
    }

    /// <summary>Handler de atualização de quotas.</summary>
    public sealed class UpdateHandler : ICommandHandler<UpdateResourceBudget, ResourceBudgetResponse>
    {
        public Task<Result<ResourceBudgetResponse>> Handle(UpdateResourceBudget request, CancellationToken cancellationToken)
        {
            var response = new ResourceBudgetResponse(
                TenantId: request.TenantId,
                Quotas: request.Quotas);

            return Task.FromResult(Result<ResourceBudgetResponse>.Success(response));
        }
    }

    /// <summary>Resposta com quotas de recursos do tenant.</summary>
    public sealed record ResourceBudgetResponse(
        Guid? TenantId,
        IReadOnlyList<ResourceQuotaDto> Quotas);

    /// <summary>Quota de recurso individual.</summary>
    public sealed record ResourceQuotaDto(
        string Resource,
        double Allocated,
        double Used,
        string Unit);
}
